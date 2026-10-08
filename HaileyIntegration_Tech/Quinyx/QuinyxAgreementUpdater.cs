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
    public async Task<SyncResult> ExecuteAsync(HaileyDeatils details, string apiKey , CancellationToken ct = default)
    {
        logger.LogInformation(
            "UpdateAgreement starting for EmploymentNumber={EmploymentNumber}",
            details.HaileyEmployeeDetails.JobData?.General?.EmploymentNumber);
       
        var quinyxTemplates = await quinyxService.GetAgreementTemplatesAsync(ct: ct);
        var results = new List<SyncResult>();
        foreach (var employment in details.HaileyEmployeeDetails.JobData?.Employment?.Employments ?? [])
        {
            var quinyxAgreement = MapToQuinyxAgreement(details, employment, quinyxTemplates);

            var result = await quinyxService.UpdateAgreementAsync(quinyxAgreement, apiKey, ct);

            logger.LogInformation(
                "UpdateAgreement completed. Success={Success} EmployeeNumber={EmployeeNumber} Message={Message}",
                result.Success, result.EmployeeNumber, result.Message);

            results.Add(result);
        }

        return MergeResults(results, details.HaileyEmployeeDetails.JobData?.General?.EmploymentNumber);
    }

    private static SyncResult MergeResults(List<SyncResult> results, string? employmentNumber)
    {
        if (results.Count == 0)
        {
            return new SyncResult
            {
                Success = false,
                EmployeeNumber = employmentNumber,
                Message = "No employments found to update agreement."
            };
        }

        var errorCodes = results.Select(r => r.ErrorCode).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        return new SyncResult
        {
            Success = results.All(r => r.Success),
            EmployeeNumber = results[0].EmployeeNumber ?? employmentNumber,
            TargetSystem = results[0].TargetSystem,
            Message = string.Join(" | ", results.Select(r => r.Message).Where(m => !string.IsNullOrEmpty(m))),
            ErrorCode = errorCodes.Count > 0 ? string.Join(",", errorCodes) : null
        };
    }

    private UpdateAgreementV2 MapToQuinyxAgreement(HaileyDeatils details,EmploymentItem employment, IReadOnlyList<AgreementTemplate> templates)
    {
        var dest = new UpdateAgreementV2
        {
            badgeNo = details.HaileyEmployeeDetails.JobData?.General?.EmploymentNumber,
        };
        
        var salary = details.HaileyEmployeeDetails.Salaries?.LastOrDefault();
        var isHourly = false;
        if (salary?.History?.Count > 0)
        {
            isHourly = salary.SalaryType?.Trim().ToLower() == "hourly";

            var latestHistory = salary.History
                .Where(h => h.Date.HasValue)
                .MaxBy(h => h.Date!.Value);

            if (latestHistory is not null)
            {
                var agreementSalary = new AgreementSalary
                {
                    fromDate = latestHistory.Date!.Value.ToDateTime(TimeOnly.MinValue),
                    fromDateSpecified = true,
                };
                if (isHourly) { agreementSalary.hourlySalary = latestHistory.Amount; agreementSalary.hourlySalarySpecified = true; }
                else { agreementSalary.monthlySalary = latestHistory.Amount; agreementSalary.monthlySalarySpecified = true; }
                dest.salariesAdd = [agreementSalary];
                dest.expires = true;
                dest.expiresSpecified = true;
                
            }
        }
        dest.useTempSalary = false;

        var matchedTemplate = ResolveTemplate(salary?.SalaryType, templates);

        if (matchedTemplate is not null)
        {
            dest.templateId = matchedTemplate.id;
            dest.templateIdSpecified = true;
            //dest.extTemplateId = matchedTemplate.templateName;
        }
        else
        {
            logger.LogWarning(
                "No Quinyx agreement template matched for SalaryType={SalaryType}. extTemplateId/extAgreementId will not be set.",
                salary?.SalaryType);
        }
        var scopeHour = (decimal)employment.Terms.ScopePercentage;
        var fromDate = employment.StartDate.Value.ToDateTime(TimeOnly.MinValue);
        
     
        if (isHourly)
        {
            dest.hourly = true;
            dest.fullEmploymentHrs = scopeHour;
            dest.fullEmploymentHrsSpecified = true;
            dest.hourlySpecified = true;
            dest.employmentRatesAdd = [new EmploymentRate { fromDate = fromDate, rate = scopeHour *100 }];
        }
        else
        {
            dest.hourly = false;
            dest.hourlySpecified = true;
            dest.fullEmploymentHrs = scopeHour;
            dest.fullEmploymentHrsSpecified = true;
            dest.employmentRatesAdd = [new EmploymentRate { fromDate = fromDate, rate = scopeHour*100 }];
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
            dest.expires = true;
            dest.expiresSpecified = true;
        }

        return dest;
    }

    private AgreementTemplate? ResolveTemplate(
        string? salaryType,
        IReadOnlyList<AgreementTemplate> templates)
    {
        return salaryType?.Trim().ToLower() switch
        {
            "hourly" => templates.FirstOrDefault(t =>t.templateName?.Contains("tim", StringComparison.OrdinalIgnoreCase) == true),
            "full-time" => templates.FirstOrDefault(t => t.templateName?.Contains("heltid", StringComparison.OrdinalIgnoreCase) == true),
            "monthly"   => templates.FirstOrDefault(t =>t.templateName?.Contains("deltid", StringComparison.OrdinalIgnoreCase) == true),
            _           => null
        };
    }
}
