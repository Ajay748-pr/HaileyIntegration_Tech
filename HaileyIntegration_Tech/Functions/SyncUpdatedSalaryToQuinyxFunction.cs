using System.Net;
using System.Text.Json;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Quinyx;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HaileyIntegration.Tech.Functions;

public sealed class SyncUpdatedSalaryToQuinyxFunction(
    QuinyxSalaryUpdater salaryUpdater,
    ILogger<SyncUpdatedSalaryToQuinyxFunction> logger)
{
    [Function(nameof(SyncUpdatedSalaryToQuinyxFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync/quinyx/salary")]
        HttpRequestData req,
        CancellationToken ct)
    {
        logger.LogInformation(
            "SyncUpdatedSalaryToQuinyxFunction triggered. RequestId={RequestId}", req.FunctionContext.InvocationId);

        HaileyDeatils? haileyDeatils;
        try
        {
            haileyDeatils = await JsonSerializer.DeserializeAsync<HaileyDeatils>(req.Body, AppJsonOptions.Inbound, ct);
        }
        catch (JsonException ex)
        {
            logger.LogWarning("Invalid request body: {Message}", ex.Message);
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync(ex.Message, ct);
            return bad;
        }

        logger.LogInformation(
            "Processing salary update for employee {EmploymentNumber}",
            haileyDeatils.HaileyEmployeeDetails.JobData.General.EmploymentNumber);

        var result = await salaryUpdater.ExecuteAsync(haileyDeatils, ct);

        var status = result.Success ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity;
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(new { updateSalary = result }, ct);
        return response;
    }
}
