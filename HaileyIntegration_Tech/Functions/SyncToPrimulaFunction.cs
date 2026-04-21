using System.Net;
using System.Text.Json;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Services;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HaileyIntegration.Tech.Functions;

public sealed class SyncToPrimulaFunction(
    IPrimulaService primulaService,
    IEmployeeMappingService mappingService,
    ILogger<SyncToPrimulaFunction> logger)
{
    [Function(nameof(SyncToPrimulaFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync/primula")] HttpRequestData req,
        CancellationToken ct)
    {
        HaileyEmployee? employee;
        try
        {
            employee = await JsonSerializer.DeserializeAsync<HaileyEmployee>(
                req.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Invalid request: {Message}", ex.Message);
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync(ex.Message, ct);
            return bad;
        }

        if (employee is null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Employee payload is required.", ct);
            return bad;
        }

        var changeType = ResolveChangeType(employee.AccountStatus, employee.EmploymentStatus);
        var canonical = mappingService.MapToCanonical(employee, changeType);
        var result = await primulaService.SyncEmployeeAsync(canonical, ct);

        var status = result.Success ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity;
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(result, ct);
        return response;
    }

    private static ChangeType ResolveChangeType(string? accountStatus, string? employmentStatus)
    {
        return employmentStatus?.ToLowerInvariant() switch
        {
            "terminated" or "resigned" or "retired" => ChangeType.Termination,
            "active" when accountStatus?.ToLowerInvariant() == "active" => ChangeType.Update,
            _ => ChangeType.Update
        };
    }
}
