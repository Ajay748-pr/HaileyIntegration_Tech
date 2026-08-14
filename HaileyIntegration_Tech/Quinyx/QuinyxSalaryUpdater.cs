using FuzzySharp;
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

        return MapToQuinyxAgreement(details, ct);        
    }

    private string MapApiKey(HaileyDeatils details, CancellationToken ct)
    {
        var department = details.HaileyCompany.Departments.FirstOrDefault(x => x.Id == details.HaileyEmployeeDetails.JobData.Employment.Employments.FirstOrDefault().OrganizationalInformation.DepartmentId);
        var units = quinyxService.GetUnitsAPIKeyAsync(ct);
        if (!quinyxService.quinyxGroups.Contains(department.Name))
        {
            return null;
        }
        var matchedUnit = units.Result.FirstOrDefault(u => Fuzz.Ratio(u.name, department.Name) >= 70);
        if (matchedUnit is null)
        {
            var belongstoDepartment = details.HaileyCompany.Departments.FirstOrDefault(x => x.Id == department.BelongingToDepartmentId);
            matchedUnit = units.Result.FirstOrDefault(u => Fuzz.Ratio(u.name, belongstoDepartment.Name) >= 70);
        }
        return matchedUnit?.API_key ?? "";

    }
    private SyncResult MapToQuinyxAgreement(HaileyDeatils details, CancellationToken ct)
    {
        var apiKey = string.Empty;
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
                .Where(h => h.Date.HasValue && h.Date >= DateOnly.FromDateTime(DateTime.Today))
                .MaxBy(h => h.Date!.Value);

            if (latestHistory is not null)
            {
                apiKey = MapApiKey(details, ct);

                if (string.IsNullOrEmpty(apiKey))
                {
                    return new SyncResult
                    {
                        Success = false,
                        EmployeeNumber = details.HaileyEmployeeDetails.JobData.General.EmploymentNumber,
                        TargetSystem = "Quinyx",
                        ErrorCode = "404",
                        Message = "Department not found"
                    };
                }

                var quinyxtemplates = quinyxService.GetAgreementTemplatesAsync(ct: ct).Result;

                var agreementSalary = new AgreementSalary
                {
                    fromDate = latestHistory.Date!.Value.ToDateTime(TimeOnly.MinValue),
                    fromDateSpecified = true,
                };
                if (isHourly) { agreementSalary.hourlySalary = latestHistory.Amount; agreementSalary.hourlySalarySpecified = true; }
                else { agreementSalary.monthlySalary = latestHistory.Amount; agreementSalary.monthlySalarySpecified = true; }
                dest.salariesAdd = [agreementSalary];
            }
            else
                return new SyncResult
                {
                    Success = false,
                    EmployeeNumber = details.HaileyEmployeeDetails.JobData.General.EmploymentNumber,
                    TargetSystem = "Quinyx",
                    ErrorCode = "400",
                    Message = "No new salary to update"
                };
        }

        dest.useTempSalary = false;

        var result = quinyxService.UpdateAgreementAsync(dest, apiKey,ct).Result;

        logger.LogInformation(
            "UpdateAgreement completed. Success={Success} EmployeeNumber={EmployeeNumber} Message={Message}",
            result.Success, result.EmployeeNumber, result.Message);
        if (isHourly)
        {
            dest.hourly = true;
            dest.fullEmploymentHrs = 0m;
            dest.fullEmploymentHrsSpecified = true;
        }
        else
        {
            dest.hourly = false;
            dest.fullEmploymentHrs = details.HaileyEmployeeDetails.JobData.Employment.Employments[0].Terms.ScopePercentage ?? 0m;
            dest.fullEmploymentHrsSpecified = true;
        }

        //if (details.HaileyEmployeeDetails.JobData?.Employment?.DateOfJoining.HasValue == true)
        //{
        //    dest.fromDate = details.HaileyEmployeeDetails.JobData.Employment.DateOfJoining.Value.ToDateTime(TimeOnly.MinValue);
        //    dest.fromDateSpecified = true;
        //}

        //if (details.HaileyEmployeeDetails.JobData?.Employment?.LastDayOfEmployment.HasValue == true)
        //{
        //    dest.toDate = details.HaileyEmployeeDetails.JobData.Employment.LastDayOfEmployment.Value.ToDateTime(TimeOnly.MinValue);
        //    dest.toDateSpecified = true;
        //}

        dest.expires = true;
        dest.expiresSpecified = true;

        return new SyncResult
        {
            Success = false,
            EmployeeNumber = details.HaileyEmployeeDetails.JobData.General.EmploymentNumber,
            TargetSystem = "Quinyx",
            ErrorCode = "200",
            Message = "Agreement sent"
        };
    }
}
