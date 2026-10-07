using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Extensions.Logging;
using ServiceReference1;
using FuzzySharp;

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
        if (string.IsNullOrEmpty(apiKey))
        {
            apiKey = MapApiKey(details, ct);
        }
        var quinyxTemplates = await quinyxService.GetAgreementTemplatesAsync(ct: ct);
        var quinyxAgreement = MapToQuinyxAgreement(details, quinyxTemplates);
        
        var result = await quinyxService.UpdateAgreementAsync(quinyxAgreement, apiKey, ct);

        logger.LogInformation(
            "UpdateAgreement completed. Success={Success} EmployeeNumber={EmployeeNumber} Message={Message}",
            result.Success, result.EmployeeNumber, result.Message);

        return result;
    }


    private string MapApiKey(HaileyDeatils details, CancellationToken ct)
    {
        var department = details.HaileyCompany.Departments.FirstOrDefault(x => x.Id == details.HaileyEmployeeDetails.JobData.Employment.Employments[0].OrganizationalInformation.DepartmentId);
        var units = quinyxService.GetUnitsAPIKeyAsync(ct).Result;
        if (!quinyxService.quinyxGroups.ToLower().Contains(department.Name.ToLower()))
        {
            return null;
        }

        var matchedUnit = units.FirstOrDefault(u => Fuzz.Ratio(u.name, department.Name) >= 80);
        if (matchedUnit is null)
        {
            var belongstoDepartment = details.HaileyCompany.Departments.FirstOrDefault(x => x.Id == department.BelongingToDepartmentId);
            matchedUnit = units.FirstOrDefault(u => Fuzz.Ratio(u.name, belongstoDepartment.Name) >= 70);
        }
        return matchedUnit?.API_key ?? "";

    }

    private UpdateAgreementV2 MapToQuinyxAgreement(HaileyDeatils details, IReadOnlyList<AgreementTemplate> templates)
    {
        var dest = new UpdateAgreementV2
        {
            badgeNo = details.HaileyEmployeeDetails.JobData?.General?.EmploymentNumber,
        };
        var employment = details.HaileyEmployeeDetails.JobData.Employment.Employments.Where(x => x.EndDate == null || x.EndDate > DateOnly.FromDateTime(DateTime.Today));
       
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
        var fromDate = DateTime.Now;
        var scopeHour = 0m;
        if(employment.Any(x=>x.EndDate is null))
        { 
            scopeHour = (decimal)employment.FirstOrDefault(x => x.EndDate is null).Terms.ScopePercentage;
            fromDate = employment.FirstOrDefault(x => x.EndDate is null).StartDate.Value.ToDateTime(TimeOnly.MinValue);
        }
            
        else if (employment.Where(x => x.EndDate.HasValue).Max(x => x.EndDate).HasValue)
        {
            scopeHour = (decimal)employment.Where(x => x.EndDate.HasValue).OrderByDescending(x => x.EndDate).FirstOrDefault()?.Terms.ScopePercentage;
            fromDate = (DateTime)(employment.Where(x => x.EndDate.HasValue).OrderByDescending(x => x.EndDate).FirstOrDefault()?.StartDate.Value.ToDateTime(TimeOnly.MinValue));
        }
     
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
