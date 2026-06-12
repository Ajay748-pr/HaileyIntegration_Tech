using System.Net;
using System.Text.Json;
using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ServiceReference1;

namespace HaileyIntegration.Tech.Functions;

public sealed class MoveEmployeeFunction(
    IQuinyxService quinyxService,
    ILogger<MoveEmployeeFunction> logger)
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    [Function(nameof(MoveEmployeeFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(
            AuthorizationLevel.Function,
            "post",
            Route = "quinyx/moveemployee")]
        HttpRequestData req,
        CancellationToken ct)
    {
        logger.LogInformation(
            "MoveEmployeeFunction triggered. RequestId={RequestId}",
            req.FunctionContext.InvocationId);

        var request =
            await JsonSerializer.DeserializeAsync<HaileyMoveEmployee>(
                req.Body,
                JsonOpts,
                ct);

        if (request is null)
        {
            logger.LogWarning("Request body was empty or could not be deserialised.");
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("A valid MoveEmployee JSON payload is required.", ct);
            return bad;
        }

        if (string.IsNullOrWhiteSpace(request.EmploymentNumber))
        {
            logger.LogWarning("Missing required field: employmentNumber.");
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("employmentNumber is required.", ct);
            return bad;
        }

        if (string.IsNullOrWhiteSpace(request.UnitExtCode))
        {
            logger.LogWarning("Missing required field: unitExtCode.");
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("unitExtCode is required.", ct);
            return bad;
        }

        if (string.IsNullOrWhiteSpace(request.NewUnitStartDate))
        {
            logger.LogWarning("Missing required field: newUnitStartDate.");
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("newUnitStartDate is required.", ct);
            return bad;
        }

        logger.LogInformation(
            "Received move request for EmploymentNumber={EmploymentNumber} UnitExtCode={UnitExtCode} NewUnitStartDate={NewUnitStartDate}",
            request.EmploymentNumber, request.UnitExtCode, request.NewUnitStartDate);

        var moveRequest = MapToQuinyxMoveEmployee(request);

        var result = await quinyxService.MoveEmployeeAsync(moveRequest, ct);

        logger.LogInformation(
            "MoveEmployee completed. Success={Success} EmployeeNumber={EmployeeNumber} Message={Message}",
            result.Success, result.EmployeeNumber, result.Message);

        var status = result.Success ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity;
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(result, ct);
        return response;
    }

    private static moveEmployee MapToQuinyxMoveEmployee(HaileyMoveEmployee src)
    {
        return new moveEmployee
        {
            badgeNo               = src.EmploymentNumber,
            unitExtCode           = src.UnitExtCode,
            newUnitStartDate      = src.NewUnitStartDate,
            oldUnitEndShareDate   = src.OldUnitEndShareDate,
            sharableOnNewUnitFrom = src.SharableOnNewUnitFrom,
            section               = src.Section,
            costCentre            = src.CostCentre,
            reportingTo           = src.ReportingTo,
        };
    }
}