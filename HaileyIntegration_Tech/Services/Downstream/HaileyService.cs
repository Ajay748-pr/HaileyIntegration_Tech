using System.Net.Http.Json;
using System.Text.Json;
using HaileyIntegration.Tech.Models;
using Microsoft.Extensions.Logging;

namespace HaileyIntegration.Tech.Services.Downstream;

public sealed class HaileyService(
    HttpClient http,
    ILogger<HaileyService> logger) : IHaileyService
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<HaileyEmployee>> GetEmployeesAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Fetching employees from Hailey: GET /Employees");

        var response = await http.GetAsync("Employees", ct);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync(ct);
        //logger.LogInformation("Hailey /Employees raw response: {Raw}", raw);

        try
        {
            var employees = System.Text.Json.JsonSerializer.Deserialize<List<HaileyEmployee>>(raw, JsonOpts);
            logger.LogInformation("Hailey returned {Count} employee(s)", employees?.Count ?? 0);
            return employees ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize Hailey /Employees response. Raw (first 500): {Raw}",
                raw.Length > 500 ? raw[..500] : raw);
            throw;
        }
    }

    public async Task<HaileyCompany> GetCompanyAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Fetching company data from Hailey: GET /Company");

        var response = await http.GetAsync("Company", ct);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync(ct);
        //logger.LogInformation("Hailey /Company raw response (first 500 chars): {Raw}",
        //    raw.Length > 500 ? raw[..500] : raw);

        try
        {
            var company = System.Text.Json.JsonSerializer.Deserialize<HaileyCompany>(raw, JsonOpts);
            logger.LogInformation(
                "Hailey company data fetched. CostCenters={CostCenters} Departments={Departments}",
                company?.CostCenters?.Count ?? 0,
                company?.Departments?.Count ?? 0);
            return company ?? new HaileyCompany();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize Hailey /Company response. Raw (first 500): {Raw}",
                raw.Length > 500 ? raw[..500] : raw);
            throw;
        }
    }
}
