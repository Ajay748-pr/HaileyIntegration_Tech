using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Extensions.Logging;
using ServiceReference1;

namespace HaileyIntegration.Tech.Quinyx;

public sealed class QuinyxSalaryUpdater(
    IQuinyxService quinyxService,
    ILogger<QuinyxSalaryUpdater> logger)
{
    public async Task<SyncResult> ExecuteAsync(HaileyDeatils details,CancellationToken ct = default)
    {
        logger.LogInformation(
            "UpdateAgreement starting for EmploymentNumber={EmploymentNumber}",
            details.HaileyEmployeeDetails.JobData?.General?.EmploymentNumber);
        var units = await quinyxService.GetUnitsAPIKeyAsync(ct);
        var departmentId = details.HaileyEmployee.DepartmentId;
        var matchedUnit = units.FirstOrDefault(u => u.name == departmentId);
        if (matchedUnit is null)
            logger.LogWarning("No Quinyx unit matched DepartmentId={DepartmentId}", departmentId);
        string apiKey = matchedUnit?.API_key ?? "";

        var quinyxtemplates = await quinyxService.GetAgreementTemplatesAsync(ct: ct);
        var quinyxAgreement = MapToQuinyxAgreement(details, quinyxtemplates);

        var result = await quinyxService.UpdateAgreementAsync(quinyxAgreement, apiKey, ct);

        logger.LogInformation(
            "UpdateAgreement completed. Success={Success} EmployeeNumber={EmployeeNumber} Message={Message}",
            result.Success, result.EmployeeNumber, result.Message);

        return result;
    }

    private UpdateAgreementV2 MapToQuinyxAgreement(HaileyDeatils details, IReadOnlyList<AgreementTemplate> quinyxtemplates)
    {
        var dest = new UpdateAgreementV2
        {
            badgeNo = details.HaileyEmployeeDetails.JobData?.General?.EmploymentNumber,
        };

        var salary = details.HaileyEmployeeDetails.Salaries?.FirstOrDefault();
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
            }
        }

        dest.useTempSalary = false;


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

        if (details.HaileyEmployeeDetails.JobData?.Employment?.DateOfJoining.HasValue == true)
        {
            dest.fromDate = details.HaileyEmployeeDetails.JobData.Employment.DateOfJoining.Value.ToDateTime(TimeOnly.MinValue);
            dest.fromDateSpecified = true;
        }

        //if (details.HaileyEmployeeDetails.JobData?.Employment?.LastDayOfEmployment.HasValue == true)
        //{
        //    dest.toDate = details.HaileyEmployeeDetails.JobData.Employment.LastDayOfEmployment.Value.ToDateTime(TimeOnly.MinValue);
        //    dest.toDateSpecified = true;
        //}

        return dest;
    }
}
