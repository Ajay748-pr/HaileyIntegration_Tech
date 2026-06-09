using System.Net;
using System.Text.Json;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ServiceReference1;

namespace HaileyIntegration.Tech.Functions;

public sealed class UpdateAgreementFunction(
    IQuinyxService quinyxService,
    ILogger<UpdateAgreementFunction> logger)
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    [Function(nameof(UpdateAgreementFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(
            AuthorizationLevel.Function,
            "post",
            Route = "quinyx/updateagreement")]
        HttpRequestData req,
        CancellationToken ct)
    {
        logger.LogInformation(
            "UpdateAgreementFunction triggered. RequestId={RequestId}",
            req.FunctionContext.InvocationId);

        var agreement = await ReadRequestAsync(req, ct);

        if (agreement is null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);

            await bad.WriteStringAsync(
                "A valid HaileyAgreement payload is required.",
                ct);

            return bad;
        }

        logger.LogInformation(
            "Received agreement request for EmployeeNumber={EmployeeNumber}",
            agreement.EmploymentNumber);

        var quinyxAgreement =
            MapToQuinyxAgreement(agreement);

        var result =
            await quinyxService.UpdateAgreementAsync(
                quinyxAgreement,
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

    private async Task<HaileyAgreement?> ReadRequestAsync(
        HttpRequestData req,
        CancellationToken ct)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<HaileyAgreement>(
                req.Body,
                JsonOpts,
                ct);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "Agreement deserialization failed");

            return null;
        }
    }

    private UpdateAgreementV2 MapToQuinyxAgreement(
        HaileyAgreement src)
    {
        var dest = new UpdateAgreementV2
        {
            badgeNo = src.EmploymentNumber,
            extAgreementId = src.ExternalAgreementId,
            extTemplateId = src.ExternalTemplateId
        };

        if (src.FromDate.HasValue)
        {
            dest.fromDate =
                src.FromDate.Value.ToDateTime(TimeOnly.MinValue);

            dest.fromDateSpecified = true;
        }

        if (src.ToDate.HasValue)
        {
            dest.toDate =
                src.ToDate.Value.ToDateTime(TimeOnly.MinValue);

            dest.toDateSpecified = true;
        }

        if (src.Expires.HasValue)
        {
            dest.expires = src.Expires.Value;
            dest.expiresSpecified = true;
        }

        if (src.EmploymentRate.HasValue &&
            src.FromDate.HasValue)
        {
            dest.employmentRatesAdd =
            [
                new EmploymentRate
                {
                    fromDate = src.FromDate.Value.ToDateTime(TimeOnly.MinValue),
                    rate = src.EmploymentRate.Value
                }
            ];
        }

        if (src.HourlySalary.HasValue &&
            src.FromDate.HasValue)
        {
            dest.salariesAdd =
            [
                new AgreementSalary
                {
                    fromDate = src.FromDate.Value.ToDateTime(TimeOnly.MinValue),
                    hourlySalary = src.HourlySalary.Value
                }
            ];
        }

        return dest;
    }
}