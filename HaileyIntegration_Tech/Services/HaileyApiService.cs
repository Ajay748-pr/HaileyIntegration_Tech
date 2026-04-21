using System.Net.Http.Headers;
using System.Net.Http.Json;
using HaileyIntegration.Tech.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HaileyIntegration.Tech.Services;

public sealed class HaileyApiService(
    HttpClient http,
    IConfiguration config,
    ILogger<HaileyApiService> logger) : IHaileyApiService
{
    public async Task<List<HaileyEmployee>> GetAllEmployeesAsync(CancellationToken ct = default)
    {
        var apiKey = config["Hailey:ApiKey"]
            ?? throw new InvalidOperationException(
                "Hailey:ApiKey is missing. Add it to local.settings.json for local dev.");

        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        logger.LogInformation("Calling Hailey Employee API: GET {BaseUrl}Employees", http.BaseAddress);

        var employees = await http.GetFromJsonAsync<List<HaileyEmployee>>("Employees", ct);

        if (employees is null)
            throw new InvalidOperationException("Hailey API returned null — check the base URL and API key.");

        logger.LogInformation("Hailey API returned {Count} employee records", employees.Count);
        return employees;
    }
}
