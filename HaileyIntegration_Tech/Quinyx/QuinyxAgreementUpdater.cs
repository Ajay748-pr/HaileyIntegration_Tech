using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Extensions.Logging;
using ServiceReference1;

namespace HaileyIntegration.Tech.Quinyx;

public sealed class QuinyxAgreementUpdater(
    IQuinyxService quinyxService,
    ILogger<QuinyxAgreementUpdater> logger)
{
    public async Task<SyncResult> ExecuteAsync(HaileyAgreement agreement, CancellationToken ct = default)
    {
        logger.LogInformation(
            "UpdateAgreement starting for EmploymentNumber={EmploymentNumber}", agreement.EmploymentNumber);

        // Fetch the existing Quinyx agreement ID so the update targets the correct record.
        // If no agreement exists yet, proceed without an id — Quinyx will create a new one.
        var agreementId = await quinyxService.GetAgreementIdAsync(agreement.EmploymentNumber!, ct);

        var quinyxAgreement = MapToQuinyxAgreement(agreement);

        if (agreementId.HasValue)
        {
            quinyxAgreement.id          = agreementId.Value;
            quinyxAgreement.idSpecified = true;
            logger.LogInformation(
                "Existing agreement found (id={Id}) for badgeNo={BadgeNo} — updating",
                agreementId.Value, agreement.EmploymentNumber);
        }
        else
        {
            logger.LogInformation(
                "No existing agreement found for badgeNo={BadgeNo} — Quinyx will create a new one",
                agreement.EmploymentNumber);
        }

        var result = await quinyxService.UpdateAgreementAsync(quinyxAgreement, ct);

        logger.LogInformation(
            "UpdateAgreement completed. Success={Success} EmployeeNumber={EmployeeNumber} Message={Message}",
            result.Success, result.EmployeeNumber, result.Message);

        return result;
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

            isMainAgreement          = true,
            isMainAgreementSpecified = true,
            hourly                   = false,
            hourlySpecified          = true,

            fullEmploymentHrs          = 40m,
            fullEmploymentHrsSpecified = true,
        };

        if (src.FromDate.HasValue)
        {
            dest.fromDate          = src.FromDate.Value.ToDateTime(TimeOnly.MinValue);
            dest.fromDateSpecified = true;
        }

        var isFixedTerm = src.EmploymentType is "FixedTerm" or "ProbationaryPeriod";
        dest.expires          = src.Expires ?? isFixedTerm;
        dest.expiresSpecified = true;

        if (src.ToDate.HasValue)
        {
            dest.toDate          = src.ToDate.Value.ToDateTime(TimeOnly.MinValue);
            dest.toDateSpecified = true;
        }

        if (src.ScopeHours.HasValue)
        {
            dest.minHrsWeek          = src.ScopeHours.Value;
            dest.minHrsWeekSpecified = true;
        }

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
