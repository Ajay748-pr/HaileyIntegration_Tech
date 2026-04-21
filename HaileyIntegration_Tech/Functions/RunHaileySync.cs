using System.Net;
using System.Text.Json;
using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HaileyIntegration.Tech.Functions;

/// <summary>
/// Local-run entry point.
/// Calls Hailey API directly, filters the 6-hour delta, and returns the result.
/// In production the Logic App orchestrates these steps separately.
/// Trigger: POST http://localhost:7071/api/sync/run
///          (optional body: { "windowHours": 6 })
/// </summary>
public sealed class RunHaileySync(
    IHaileyApiService haileyApi,
    IEmployeeFilterService filterService,
    ILogger<RunHaileySync> logger)
{
    [Function(nameof(RunHaileySync))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync/run")] HttpRequestData req,
        CancellationToken ct)
    {
        // Optional override body: { "windowHours": 12 }
        int windowHours = 6;
        try
        {
            var body = await JsonSerializer.DeserializeAsync<RunSyncRequest>(
                req.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                ct);
            if (body?.WindowHours > 0) windowHours = body.WindowHours;
        }
        catch
        {
            // body is optional — ignore parse errors
        }

        logger.LogInformation("RunHaileySync started. Window: {Hours} hours", windowHours);

        List<Models.HaileyEmployee> allEmployees;
        try
        {
            allEmployees = await haileyApi.GetAllEmployeesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch employees from Hailey API");
            var err = req.CreateResponse(HttpStatusCode.BadGateway);
            await err.WriteAsJsonAsync(new { error = "Hailey API call failed.", detail = ex.Message }, ct);
            return err;
        }

        var filterResult = filterService.FilterByLastUpdated(new FilterEmployeesRequest
        {
            Employees = allEmployees,
            WindowHours = windowHours
        });

        logger.LogInformation(
            "Sync complete. {Filtered}/{Total} employees updated since {WindowStart:u}",
            filterResult.FilteredCount, filterResult.TotalReceived, filterResult.WindowStart);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            runTime = DateTime.UtcNow,
            windowHours,
            totalReceived = filterResult.TotalReceived,
            filteredCount = filterResult.FilteredCount,
            windowStart = filterResult.WindowStart,
            windowEnd = filterResult.WindowEnd,
            employees = filterResult.Employees
        }, ct);
        return response;
    }

    private sealed class RunSyncRequest
    {
        public int WindowHours { get; set; }
    }
}
