using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Extensions.Logging;
using ServiceReference1;

namespace HaileyIntegration.Tech.Quinyx;

public sealed class QuinyxAgreementUpdater(
    IQuinyxService quinyxService,
    ILogger<QuinyxAgreementUpdater> logger)
{
    public async Task<SyncResult> ExecuteAsync(HaileyDeatils details, CancellationToken ct = default)
    {
        logger.LogInformation(
            "UpdateAgreement starting for EmploymentNumber={EmploymentNumber}",
            details.HaileyEmployeeDetails.JobData?.General?.EmploymentNumber);

        var templates = await quinyxService.GetAgreementTemplatesAsync(ct: ct);
        var quinyxAgreement = MapToQuinyxAgreement(details, templates);

        var result = await quinyxService.UpdateAgreementAsync(quinyxAgreement, ct);

        logger.LogInformation(
            "UpdateAgreement completed. Success={Success} EmployeeNumber={EmployeeNumber} Message={Message}",
            result.Success, result.EmployeeNumber, result.Message);

        return result;
    }

    private UpdateAgreementV2 MapToQuinyxAgreement(HaileyDeatils details, IReadOnlyList<AgreementTemplate> templates)
    {
        var dest = new UpdateAgreementV2
        {
            badgeNo = details.HaileyEmployeeDetails.JobData?.General?.EmploymentNumber,
        };

        var firstSalary = details.HaileyEmployeeDetails.Salaries?.FirstOrDefault();
        var isHourly = false;
        if (firstSalary?.History?.Count > 0)
        {
            isHourly = firstSalary.SalaryType?.Trim().ToLower() == "hourly";

            dest.salariesAdd = [.. firstSalary.History
                .Where(h => h.Date.HasValue)
                .Select(h =>
                {
                    var s = new AgreementSalary
                    {
                        fromDate          = h.Date!.Value.ToDateTime(TimeOnly.MinValue),
                        fromDateSpecified = true,
                    };
                    if (isHourly) { s.hourlySalary  = h.Amount; s.hourlySalarySpecified  = true; }
                    else          { s.monthlySalary = h.Amount; s.monthlySalarySpecified = true; }
                    return s;
                })];
        }

        dest.useTempSalary = false;

        var matchedTemplate = ResolveTemplate(firstSalary?.SalaryType, templates);

        if (matchedTemplate is not null)
        {
            dest.templateId = matchedTemplate.id;
        }
        else
        {
            logger.LogWarning(
                "No Quinyx agreement template matched for SalaryType={SalaryType}. extTemplateId/extAgreementId will not be set.",
                firstSalary?.SalaryType);
        }

        if (isHourly)
        {
            dest.hourly = true;
            dest.fullEmploymentHrs = 0m;
            dest.fullEmploymentHrsSpecified = true;
        }
        else
        {
            dest.hourly = false;
            dest.fullEmploymentHrs = details.HaileyEmployee.ScopePercentage ?? 0m;
            dest.fullEmploymentHrsSpecified = true;
        }
        var employmentDateOfJoining = details.HaileyEmployeeDetails.JobData?.Employment?.DateOfJoining;
        var scopePercentage = details.HaileyEmployee.ScopePercentage;
        if (employmentDateOfJoining.HasValue && scopePercentage.HasValue)
        {
            dest.employmentRatesAdd =
            [
                new EmploymentRate
                {
                    fromDate = employmentDateOfJoining.Value.ToDateTime(TimeOnly.MinValue),
                    rate     = scopePercentage.Value
                }
            ];
        }

        if (details.HaileyEmployeeDetails.JobData?.Employment?.DateOfJoining.HasValue == true)
        {
            dest.fromDate = details.HaileyEmployeeDetails.JobData.Employment.DateOfJoining.Value.ToDateTime(TimeOnly.MinValue);
            dest.fromDateSpecified = true;
        }

        if (details.HaileyEmployeeDetails.JobData?.Employment?.LastDayOfEmployment.HasValue == true)
        {
            dest.toDate = details.HaileyEmployeeDetails.JobData.Employment.LastDayOfEmployment.Value.ToDateTime(TimeOnly.MinValue);
            dest.toDateSpecified = true;
        }

        return dest;
    }

    private AgreementTemplate? ResolveTemplate(
        string? salaryType,
        IReadOnlyList<AgreementTemplate> templates)
    {
        return salaryType?.Trim().ToLower() switch
        {
            "hourly"    => templates.FirstOrDefault(t => t.hourly == 1),
            "full-time" => templates.FirstOrDefault(t => t.hourly == 0
                               && t.templateName?.Contains("heltid", StringComparison.OrdinalIgnoreCase) == true),
            "monthly"   => templates.FirstOrDefault(t => t.hourly == 0
                               && t.templateName?.Contains("deltid", StringComparison.OrdinalIgnoreCase) == true),
            _           => null
        };
    }
}
