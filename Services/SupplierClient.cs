using System.Net;
using System.Text.Json;
using nhom1_sales_and_inventory_management.DTOs.Integration;

namespace nhom1_sales_and_inventory_management.Services;

public class SupplierClient : ISupplierClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly string _internalApiKey;

    public SupplierClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _internalApiKey = configuration["Services:InternalApiKey"]
            ?? throw new InvalidOperationException("Services:InternalApiKey is not configured.");
    }

    public async Task<SupplierDto?> GetByIdAsync(int id)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/internal/suppliers/{id}");
        request.Headers.Add("X-Internal-Api-Key", _internalApiKey);
        using var response = await _http.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var wrapped = JsonSerializer.Deserialize<ApiResponse<SupplierDto>>(content, JsonOptions);
        return wrapped?.Data;
    }
}
