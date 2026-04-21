using System.Net;
using System.Text.Json;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Services;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HaileyIntegration.Tech.Functions;

public sealed class SyncToQuinyxFunction(
    IQuinyxService quinyxService,
    IEmployeeMappingService mappingService,
    ILogger<SyncToQuinyxFunction> logger)
{
    [Function(nameof(SyncToQuinyxFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync/quinyx")] HttpRequestData req,
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
            logger.LogWarning("Invalid Quinyx sync request: {Message}", ex.Message);
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

        logger.LogInformation("Syncing employee {EmployeeNumber} to Quinyx", employee.EmploymentNumber);

        var canonical = mappingService.MapToCanonical(employee, ChangeType.Update);
        var result = await quinyxService.SyncEmployeeAsync(canonical, ct);

        var status = result.Success ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity;
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(result, ct);
        return response;
    }
}
