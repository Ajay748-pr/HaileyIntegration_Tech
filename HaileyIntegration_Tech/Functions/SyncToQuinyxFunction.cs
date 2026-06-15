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

        HaileyEmployee? employee;
        try
        {
            employee = await JsonSerializer.DeserializeAsync<HaileyEmployee>(req.Body, AppJsonOptions.Inbound, ct);
        }
        catch (JsonException ex)
        {
            logger.LogWarning("Invalid request body: {Message}", ex.Message);
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync(ex.Message, ct);
            return bad;
        }

        if (employee is null || string.IsNullOrWhiteSpace(employee.EmploymentNumber))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("A valid employee payload with employmentNumber is required.", ct);
            return bad;
        }

        logger.LogInformation(
            "Processing employee {EmploymentNumber} → UpdateEmployee then UpdateAgreement",
            employee.EmploymentNumber);

        // Step 1 — UpdateEmployee
        var empResult = await employeeUpdater.ExecuteAsync(employee, ct);

        if (!empResult.Success)
        {
            logger.LogWarning(
                "UpdateEmployee failed for {EmploymentNumber}: {Message}",
                employee.EmploymentNumber, empResult.Message);

            var failResponse = req.CreateResponse(HttpStatusCode.UnprocessableEntity);
            await failResponse.WriteAsJsonAsync(new
            {
                updateEmployee  = empResult,
                updateAgreement = (object?)null
            }, ct);
            return failResponse;
        }

        // Step 2 — UpdateAgreement (derived from the same employee payload)
        var agreement = BuildAgreement(employee);
        var agreeResult = await agreementUpdater.ExecuteAsync(agreement, ct);

        var status = agreeResult.Success ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity;
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(new
        {
            updateEmployee  = empResult,
            updateAgreement = agreeResult
        }, ct);
        return response;
    }

    private static HaileyAgreement BuildAgreement(HaileyEmployee src) => new()
    {
        EmploymentNumber = src.EmploymentNumber,
        EmploymentType   = src.EmploymentType,
        FromDate         = src.DateOfJoining,
        ToDate           = src.LastDayOfEmployment,
        Expires          = src.LastDayOfEmployment.HasValue,
        ScopeHours       = src.ScopeHours,
        EmploymentRate   = src.ScopePercentage,
    };
}
