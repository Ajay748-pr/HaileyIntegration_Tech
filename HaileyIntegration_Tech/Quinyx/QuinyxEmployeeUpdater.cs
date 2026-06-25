using FuzzySharp;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Extensions.Logging;
using ServiceReference1;
using System.Text.RegularExpressions;

namespace HaileyIntegration.Tech.Quinyx;

public sealed partial class QuinyxEmployeeUpdater(
    IQuinyxService quinyxService,
    ILogger<QuinyxEmployeeUpdater> logger)
{
    [GeneratedRegex(@"[^0-9A-Za-z]")]
    private static partial Regex NonAlphanumericRegex();
    public async Task<SyncResult> ExecuteAsync(HaileyDeatils details, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(details.HaileyEmployeeDetails.JobData.General.EmploymentNumber)
            || string.IsNullOrEmpty(details.HaileyEmployeeDetails.JobData.General.CompanyEmail)
            || string.IsNullOrEmpty(details.HaileyMangerEmployeeNumber)
            || string.IsNullOrEmpty(details.HaileyEmployee.DepartmentId)
            || details.HaileyEmployeeDetails.JobData?.Employment?.DateOfJoining == null)
        {
            logger.LogError("Required fields are missing.");
            return new SyncResult
            {
                Success = false,
                EmployeeNumber = details.HaileyEmployeeDetails.JobData.General.EmploymentNumber,
                TargetSystem = "Quinyx",
                ErrorCode = "404",
                Message = "required fields are missing"
            };
        }

        logger.LogInformation(
            "UpdateEmployee starting for EmploymentNumber={EmploymentNumber}", details.HaileyEmployeeDetails.JobData.General.EmploymentNumber);

        var apiKey = MapApiKey(details, ct);

        if (apiKey == null)
        {
            return new SyncResult
            {
                Success = false,
                EmployeeNumber = details.HaileyEmployeeDetails.JobData.General.EmploymentNumber,
                TargetSystem = "Quinyx",
                ErrorCode = "404",
                Message = "No matching unit found"
            };
        }
        var categories = await quinyxService.GetCategoriesAsync(ct: ct);
        var staffCategory = categories.FirstOrDefault(c => c.categoryName == "Beh. app Medarbetare");
        if (staffCategory is null)
            logger.LogWarning("No Quinyx category matched 'Beh. app Medarbetare'.");

        var quinyxEmployee = MapToQuinyxEmployee(details);

        if (staffCategory is not null)
        {
            quinyxEmployee.staffCat = staffCategory.id;
            quinyxEmployee.staffCatSpecified = true;
        }

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
            givenName = src.HaileyEmployee.FirstName,
            familyName = src.HaileyEmployee.LastName,
            email = src.HaileyEmployeeDetails.JobData.General.CompanyEmail,
            phoneNo = src.HaileyEmployee.WorkPhone,
            socsecNo = src.HaileyEmployee.PersonalIdentityNumber is { } pin
                ? NonAlphanumericRegex().Replace(pin, "")
                : null,
            address1 = src.HaileyEmployee.StreetAddress,
            zip = src.HaileyEmployee.PostalCode,
            city = src.HaileyEmployee.City,
            country = src.HaileyEmployee.Country,
            reportingTo = src.HaileyMangerEmployeeNumber,
            active = 1,
            activeSpecified = true,

        };

        if (!string.IsNullOrWhiteSpace(src.HaileyEmployee.Gender))
        {
            dest.sex = src.HaileyEmployee.Gender?.Trim().ToLower() switch
            {
                "male" => 0,
                "female" => 1,
                _ => 3
            };
            dest.sexSpecified = true;
        }

        if (!string.IsNullOrWhiteSpace(src.HaileyEmployee.DateOfBirth) &&
            DateTime.TryParse(src.HaileyEmployee.DateOfBirth, out var dob))
        {
            dest.dateOfBirth = dob;
            dest.dateOfBirthSpecified = true;
        }

        if (src.HaileyEmployee.DateOfJoining.HasValue && src.HaileyEmployee.DateOfJoining > DateOnly.MinValue)
        {
            dest.employedDate = src.HaileyEmployee.DateOfJoining.Value.ToDateTime(TimeOnly.MinValue);
            dest.employedDateSpecified = true;
        }

        if (src.HaileyEmployee.LastDayOfEmployment.HasValue && src.HaileyEmployee.LastDayOfEmployment > DateOnly.MinValue)
        {
            dest.leaveDate = src.HaileyEmployee.LastDayOfEmployment.Value.ToDateTime(TimeOnly.MinValue);
            dest.leaveDateSpecified = true;

        }

        return dest;
    }

}
