using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Extensions.Logging;
using ServiceReference1;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            if (employment is null) continue;

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
        
        // Pick the salary whose history holds the most recent dated entry across all salaries.
        var latest = (details.HaileyEmployeeDetails.Salaries ?? [])
            .SelectMany(s => (s.History ?? [])
                .Where(h => h.Date.HasValue)
                .Select(h => new { Salary = s, History = h }))
            .MaxBy(x => x.History.Date!.Value);

        var salary = latest?.Salary ?? details.HaileyEmployeeDetails.Salaries?.LastOrDefault();
        var latestHistory = latest?.History;
        var isHourly = salary?.SalaryType?.Trim().ToLower() == "hourly";

        // Hourly-paid employees always have a 0% scope of employment rate.
        var scopeHour = isHourly ? 0 : employment.Terms?.ScopePercentage ?? 0;

        if (latestHistory is not null)
        {
            var agreementSalary = new AgreementSalary
            {
                fromDate = latestHistory.Date!.Value.ToDateTime(TimeOnly.MinValue),
                fromDateSpecified = true,
               
            };
            if (isHourly) { agreementSalary.hourlySalary = latestHistory.Amount; agreementSalary.hourlySalarySpecified = true; }
            else { agreementSalary.monthlySalary = latestHistory.Amount; agreementSalary.monthlySalarySpecified = true; }
            if (scopeHour!=1)
            {
                dest.expires = true;
                dest.expiresSpecified = true;
            }
            agreementSalary.monthlyHoursDivisor = 165;
            agreementSalary.monthlyHoursDivisorSpecified = true;
            dest.salariesAdd = [agreementSalary];
           
        }
        dest.useTempSalary = false;

        

        var matchedTemplate = ResolveTemplate(isHourly, scopeHour, templates);

        if (matchedTemplate is not null)
        {
            dest.templateId = matchedTemplate.id;
            dest.templateIdSpecified = true;
            //dest.extTemplateId = matchedTemplate.templateName;
        }
        else
        {
            logger.LogWarning(
                "No Quinyx agreement template matched for SalaryType={SalaryType} ScopePercentage={ScopePercentage}. templateId will not be set.",
                salary?.SalaryType, scopeHour);
        }
        dest.hourly = isHourly;
        dest.hourlySpecified = true;

        if (scopeHour != 0)
        {
            dest.fullEmploymentHrs = scopeHour;
            dest.fullEmploymentHrsSpecified = true;

            if (employment.StartDate.HasValue)
            {
                var fromDate = employment.StartDate.Value.ToDateTime(TimeOnly.MinValue);
                dest.employmentRatesAdd = [new EmploymentRate { fromDate = fromDate, rate = scopeHour * 100 }];
            }
            else
            {
                logger.LogWarning(
                    "Employment {EmploymentId} has no startDate. Employment rate will not be set.",
                    employment.EmploymentId);
            }
            if (scopeHour == 1)
            {
                dest.expires = false;
                dest.expiresSpecified = true;
            }
            else
            {
                //var lastWorkingDay = employment.LastWorkingDay;
                var endDate = employment.EndDate;
                //var toDate = lastWorkingDay.HasValue && (!endDate.HasValue || lastWorkingDay.Value < endDate.Value)
                //    ? lastWorkingDay
                //    : endDate;

                if (endDate.HasValue)
                {
                    dest.toDate = endDate.Value.ToDateTime(TimeOnly.MinValue);
                    dest.toDateSpecified = true;
                    dest.expires = false;
                    dest.expiresSpecified = true;
                }
            }
        }
       
        if (details.HaileyEmployeeDetails.JobData?.Employment?.DateOfJoining.HasValue == true)
        {
            dest.fromDate = details.HaileyEmployeeDetails.JobData.Employment.DateOfJoining.Value.ToDateTime(TimeOnly.MinValue);
            dest.fromDateSpecified = true;
        }

        return dest;
    }

    // scopePercentage is a fraction from Hailey (1 = 100%, 0.45 = 45%).
    // Hourly (Timlön)                → "Lokalt kollektivavtal månad tim"
    // Monthly (Månadslön) 0 < x < 1  → "Lokalt kollektivavtal månad deltid"--- Part-Time
    // Monthly (Månadslön) x >= 1     → "Lokalt kollektivavtal månad heltid" ---- Full- Time
    private static AgreementTemplate? ResolveTemplate(
        bool isHourly,
        decimal scopePercentage,
        IReadOnlyList<AgreementTemplate> templates)
    {
        var suffix = isHourly ? "tim"
            : scopePercentage >= 1 ? "heltid"
            : scopePercentage > 0 ? "deltid"
            : null;

        return suffix is null
            ? null
            : templates.FirstOrDefault(t => t.templateName?.Trim().EndsWith(suffix, StringComparison.OrdinalIgnoreCase) == true);
    }
}
