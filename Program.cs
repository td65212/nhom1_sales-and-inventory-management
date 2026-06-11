using Microsoft.EntityFrameworkCore;
using nhom1_sales_and_inventory_management.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// 🔥 PORT cho Render (phải đặt TRƯỚC Build)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Middleware
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();