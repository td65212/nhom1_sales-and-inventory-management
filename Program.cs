using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using nhom1_sales_and_inventory_management.Infrastructure.Data;
using nhom1_sales_and_inventory_management.Services;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var connectionString = GetDatabaseConnectionString(builder.Configuration);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHttpClient<ISupplierClient, SupplierClient>(client =>
{
    var baseUrl = builder.Configuration["Services:Order:BaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("Services:Order:BaseUrl is not configured.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey)
    || string.IsNullOrWhiteSpace(jwtIssuer)
    || string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("Jwt configuration is incomplete.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:4173",
                "http://127.0.0.1:4173",
                "https://front-end-sales-and-inventory-management.onrender.com")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static string GetDatabaseConnectionString(IConfiguration configuration)
{
    var connectionString = NormalizeConnectionString(
        configuration.GetConnectionString("DefaultConnection"));
    var databaseUrl = NormalizeConnectionString(configuration["DATABASE_URL"]);

    if (!string.IsNullOrWhiteSpace(databaseUrl))
        connectionString = ConvertDatabaseUrl(databaseUrl);

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Database connection is missing. Configure DATABASE_URL or "
            + "ConnectionStrings__DefaultConnection.");
    }

    NpgsqlConnectionStringBuilder parsedConnectionString;
    try
    {
        parsedConnectionString = new NpgsqlConnectionStringBuilder(connectionString);
    }
    catch (ArgumentException ex)
    {
        throw new InvalidOperationException(
            "Database connection string is invalid. On Render, use a single-line "
            + "PostgreSQL URL or Npgsql connection string.",
            ex);
    }

    if (!string.Equals(parsedConnectionString.Host, "localhost", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(parsedConnectionString.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
    {
        return parsedConnectionString.ConnectionString;
    }

    if (configuration["ASPNETCORE_ENVIRONMENT"] == Environments.Development)
        return parsedConnectionString.ConnectionString;

    throw new InvalidOperationException(
        "Production database cannot use localhost. Configure DATABASE_URL or "
        + "ConnectionStrings__DefaultConnection on Render.");
}

static string ConvertDatabaseUrl(string databaseUrl)
{
    databaseUrl = databaseUrl.Trim();

    if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri)
        || (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
    {
        return databaseUrl;
    }

    var credentials = uri.UserInfo.Split(':', 2);
    if (credentials.Length != 2)
        throw new InvalidOperationException("DATABASE_URL credentials are invalid.");

    return new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(credentials[0]),
        Password = Uri.UnescapeDataString(credentials[1]),
        SslMode = SslMode.Require
    }.ConnectionString;
}

static string? NormalizeConnectionString(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return value;

    var normalized = value
        .Replace("\\r", string.Empty, StringComparison.Ordinal)
        .Replace("\\n", "\n", StringComparison.Ordinal)
        .Trim();

    if (!normalized.Contains('\r') && !normalized.Contains('\n'))
        return normalized;

    var parts = normalized
        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(part => part.Trim())
        .Where(part => part.Length > 0)
        .ToArray();

    if (normalized.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        || normalized.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return string.Concat(parts);
    }

    return string.Join(
        ';',
        parts.Select(part => part.Trim(';')).Where(part => part.Length > 0));
}
