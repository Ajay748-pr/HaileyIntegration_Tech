using FuzzySharp;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Extensions.Logging;
using ServiceReference1;

namespace HaileyIntegration.Tech.Quinyx;

public sealed partial class QuinyxEmployeeDeactivate(
    IQuinyxService quinyxService,
    ILogger<QuinyxEmployeeUpdater> logger)
{
    public async Task<SyncResult> ExecuteAsync(HaileyDeatils details, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(details.HaileyEmployee.EmploymentNumber)
            || details.HaileyEmployee.LastDayOfEmployment == null)
        {
            logger.LogError("Required fields are missing.");
            return new SyncResult
            {
                Success = false,
                EmployeeNumber = details.HaileyEmployee.EmploymentNumber,
                TargetSystem = "Quinyx",
                ErrorCode = "404",
                Message = "required fields are missing"
            };
        }
        if (details.HaileyEmployee.LastDayOfEmployment.Value != DateOnly.FromDateTime(DateTime.Today))
        {
            return new SyncResult
            {
                Success = false,
                EmployeeNumber = details.HaileyEmployee.EmploymentNumber,
                TargetSystem = "Quinyx",
                ErrorCode = "400",
                Message = "lastDayOfEmployment is not equal to todays date"
            };
        }
        logger.LogInformation(
            "DeactivateEmployee starting for EmploymentNumber={EmploymentNumber}", details.HaileyEmployee.EmploymentNumber);

        var apiKey = MapApiKey(details, ct);

        if (string.IsNullOrEmpty(apiKey))
        {
            return new SyncResult
            {
                Success = false,
                EmployeeNumber = details.HaileyEmployee.EmploymentNumber,
                TargetSystem = "Quinyx",
                ErrorCode = "404",
                Message = "No matching unit found"
            };
        }

        var quinyxEmployee = MapToQuinyxEmployee(details);

        var result = await quinyxService.UpdateEmployeeAsync(quinyxEmployee, apiKey, ct);

        logger.LogInformation(
            "UpdateEmployee completed. Success={Success} EmployeeNumber={EmployeeNumber} Message={Message}",
            result.Success, result.EmployeeNumber, result.Message);

        result.ApiKey = apiKey;
        return result;
    }

    private string MapApiKey(HaileyDeatils details, CancellationToken ct)
    {
        var department = details.HaileyCompany.Departments.FirstOrDefault(x => x.Id == details.HaileyEmployee.DepartmentId);
        var units = quinyxService.GetUnitsAPIKeyAsync(ct);
        if (!quinyxService.quinyxGroups.ToLower().Contains(department.Name.ToLower()))
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

    private UpdateEmployee MapToQuinyxEmployee(HaileyDeatils src)
    {
        var dest = new UpdateEmployee
        {
            badgeNo = src.HaileyEmployee.EmploymentNumber,
            passive = 1,
            passiveSpecified = true,
            active = 0,
            activeSpecified = true,
        };
        if (src.HaileyEmployee.LastDayOfEmployment.HasValue)
        {
            dest.leaveDate = src.HaileyEmployee.LastDayOfEmployment.Value.ToDateTime(TimeOnly.MinValue);
            dest.leaveDateSpecified = true;
            
        }
        return dest;
    }

}
