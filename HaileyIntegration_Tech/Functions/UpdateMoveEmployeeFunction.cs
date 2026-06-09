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
        var request =
            await JsonSerializer.DeserializeAsync<HaileyMoveEmployee>(
                req.Body,
                JsonOpts,
                ct);

        if (request is null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);

            await bad.WriteStringAsync(
                "Valid move employee payload required.",
                ct);

            return bad;
        }

        var moveRequest =
            MapToQuinyxMoveEmployee(request);

        var result =
            await quinyxService.MoveEmployeeAsync(
                moveRequest,
                ct);

        var status =
            result.Success
                ? HttpStatusCode.OK
                : HttpStatusCode.UnprocessableEntity;

        var response =
            req.CreateResponse(status);

        await response.WriteAsJsonAsync(result, ct);

        return response;
    }

    private static moveEmployee MapToQuinyxMoveEmployee(
        HaileyMoveEmployee src)
    {
        return new moveEmployee
        {
            badgeNo = src.EmploymentNumber,
            sharableOnNewUnitFrom = src.SharableOnNewUnitFrom,
            newUnitStartDate = src.NewUnitStartDate,
            oldUnitEndShareDate = src.OldUnitEndShareDate,
            unitExtCode = src.UnitExtCode,
            reportingTo = src.ReportingTo,
            section = src.Section,
            costCentre = src.CostCentre,
            moveId = src.MoveId
        };
    }
}