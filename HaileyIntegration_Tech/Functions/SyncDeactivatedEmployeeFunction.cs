using System.Net;
using System.Text.Json;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Quinyx;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HaileyIntegration.Tech.Functions;

public sealed class SyncDeactivatedEmployeeFunction(
    QuinyxEmployeeDeactivate employeeDeactivate,
    ILogger<SyncDeactivatedEmployeeFunction> logger)
{
    [Function(nameof(SyncDeactivatedEmployeeFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync/quinyx/deactivate")]
        HttpRequestData req,
        CancellationToken ct)
    {
        logger.LogInformation(
            "SyncDeactivatedEmployeeFunction triggered. RequestId={RequestId}", req.FunctionContext.InvocationId);

        HaileyDeatils? haileyDeatils;
        try
        {
            haileyDeatils = await JsonSerializer.DeserializeAsync<HaileyDeatils>(req.Body);
        }
        catch (JsonException ex)
        {
            logger.LogWarning("Invalid request body: {Message}", ex.Message);
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync(ex.Message, ct);
            return bad;
        }

        logger.LogInformation(
            "Deactivating employee {EmploymentNumber}",
            haileyDeatils.HaileyEmployee.EmploymentNumber);

        var result = await employeeDeactivate.ExecuteAsync(haileyDeatils, ct);

        var status = result.Success ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity;
        var response = req.CreateResponse(status);
        result.ApiKey = null;
        await response.WriteAsJsonAsync(new { updateEmployee = result }, ct);
        return response;
    }
}
