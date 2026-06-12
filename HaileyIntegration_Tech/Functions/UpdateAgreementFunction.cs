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
            await bad.WriteStringAsync("A valid HaileyAgreement payload is required.", ct);
            return bad;
        }

        if (string.IsNullOrWhiteSpace(agreement.EmploymentNumber))
        {
            logger.LogWarning("Missing required field: employmentNumber.");
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("employmentNumber is required.", ct);
            return bad;
        }

        logger.LogInformation(
            "Received agreement request for EmployeeNumber={EmployeeNumber} EmploymentType={EmploymentType} ScopeHours={ScopeHours} ScopePercentage={ScopePercentage}",
            agreement.EmploymentNumber, agreement.EmploymentType, agreement.ScopeHours, agreement.EmploymentRate);

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

    private UpdateAgreementV2 MapToQuinyxAgreement(HaileyAgreement src)
    {
        var dest = new UpdateAgreementV2
        {
            badgeNo          = src.EmploymentNumber,
            extAgreementId   = src.ExternalAgreementId,
            extTemplateId    = src.ExternalTemplateId,
            name             = src.Name,
            comment          = src.Comment,
            additionalField1 = src.AdditionalField1,
            additionalField2 = src.AdditionalField2,
            additionalField3 = src.AdditionalField3,
            additionalField4 = src.AdditionalField4,
            additionalField5 = src.AdditionalField5,

            // Always the primary agreement, never hourly
            isMainAgreement          = true,
            isMainAgreementSpecified = true,
            hourly                   = false,
            hourlySpecified          = true,

            // Standard full-time week is 40 hrs; contracted hours come from scopeHours
            fullEmploymentHrs          = 40m,
            fullEmploymentHrsSpecified = true,
        };

        // fromDate → dateOfJoining
        if (src.FromDate.HasValue)
        {
            dest.fromDate          = src.FromDate.Value.ToDateTime(TimeOnly.MinValue);
            dest.fromDateSpecified = true;
        }

        // expires + toDate derived from employmentType
        // Permanent  → expires = false, no toDate
        // FixedTerm  → expires = true,  toDate = ToDate (endOfFixedTerm from Logic App)
        // ProbationaryPeriod → expires = true, toDate = ToDate (endOfProbationaryPeriod from Logic App)
        var isFixedTerm = src.EmploymentType is "FixedTerm" or "ProbationaryPeriod";

        dest.expires          = src.Expires ?? isFixedTerm;
        dest.expiresSpecified = true;

        if (src.ToDate.HasValue)
        {
            dest.toDate          = src.ToDate.Value.ToDateTime(TimeOnly.MinValue);
            dest.toDateSpecified = true;
        }

        // minHrsWeek → scopeHours (actual contracted hours/week)
        if (src.ScopeHours.HasValue)
        {
            dest.minHrsWeek          = src.ScopeHours.Value;
            dest.minHrsWeekSpecified = true;
        }

        // employmentRatesAdd → scopePercentage + fromDate
        if (src.EmploymentRate.HasValue && src.FromDate.HasValue)
        {
            dest.employmentRatesAdd =
            [
                new EmploymentRate
                {
                    fromDate = src.FromDate.Value.ToDateTime(TimeOnly.MinValue),
                    rate     = src.EmploymentRate.Value
                }
            ];
        }

        // salariesAdd → hourlySalary + fromDate
        if (src.HourlySalary.HasValue && src.FromDate.HasValue)
        {
            dest.salariesAdd =
            [
                new AgreementSalary
                {
                    fromDate     = src.FromDate.Value.ToDateTime(TimeOnly.MinValue),
                    hourlySalary = src.HourlySalary.Value
                }
            ];
        }

        return dest;
    }
}