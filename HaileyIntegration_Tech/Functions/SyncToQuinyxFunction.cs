using System.Net;
using System.Text.Json;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Quinyx;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HaileyIntegration.Tech.Functions;

public sealed class SyncToQuinyxFunction(
    QuinyxEmployeeUpdater employeeUpdater,
    QuinyxAgreementUpdater agreementUpdater,
    ILogger<SyncToQuinyxFunction> logger)
{
    [Function(nameof(SyncToQuinyxFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync/quinyx")]
        HttpRequestData req,
        CancellationToken ct)
    {
        logger.LogInformation(
            "SyncToQuinyxFunction triggered. RequestId={RequestId}", req.FunctionContext.InvocationId);

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
            "Processing employee {EmploymentNumber} → UpdateEmployee then UpdateAgreement",
             haileyDeatils.HaileyEmployeeDetails.JobData.General.EmploymentNumber);

        // Step 1 — UpdateEmployee
        var empResult = await employeeUpdater.ExecuteAsync(haileyDeatils, ct);

        if (!empResult.Success)
        {
            logger.LogWarning(
                "UpdateEmployee failed for {EmploymentNumber}: {Message}",
                empResult.Message);

            var failResponse = req.CreateResponse(HttpStatusCode.UnprocessableEntity);
            await failResponse.WriteAsJsonAsync(new
            {
                updateEmployee  = empResult,
                updateAgreement = (object?)null
            }, ct);
            return failResponse;
        }

        // Step 2 — UpdateAgreement
       
        var agreeResult = await agreementUpdater.ExecuteAsync(haileyDeatils, ct);

        var status = agreeResult.Success ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity;
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(new
        {
            updateEmployee  = empResult,
            updateAgreement = agreeResult
        }, ct);
        return response;
    }
}
