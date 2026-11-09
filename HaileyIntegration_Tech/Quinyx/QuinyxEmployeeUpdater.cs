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
    private partial Regex NonAlphanumericRegex();
    public async Task<SyncResult> ExecuteAsync(HaileyDeatils details, CancellationToken ct = default)
    {
        var employmentNumber = details.HaileyEmployeeDetails?.JobData?.General?.EmploymentNumber;
        if (string.IsNullOrEmpty(employmentNumber)
            || string.IsNullOrEmpty(details.HaileyEmployeeDetails!.JobData!.General!.CompanyEmail)
           // || string.IsNullOrEmpty(details.HaileyManagerEmployeeNumber)
            || string.IsNullOrEmpty(details.HaileyEmployeeDetails.OrganizationalBelonging?.DepartmentId)
            || details.HaileyEmployeeDetails.JobData.Employment?.DateOfJoining == null)
        {
            logger.LogError("Required fields are missing.");
            return new SyncResult
            {
                Success = false,
                EmployeeNumber = employmentNumber,
                TargetSystem = "Quinyx",
                ErrorCode = "404",
                Message = "required fields are missing"
            };
        }

        logger.LogInformation(
            "UpdateEmployee starting for EmploymentNumber={EmploymentNumber}", employmentNumber);

        var (apiKey, unit) = await MapApiKeyAsync(details, ct);
        if (string.IsNullOrEmpty(apiKey))
        {
            return new SyncResult
            {
                Success = false,
                EmployeeNumber = employmentNumber,
                TargetSystem = "Quinyx",
                ErrorCode = "404",
                Message = "No matching unit found"
            };
        }
        var categories = await quinyxService.GetCategoriesAsync(ct: ct);
        var staffCategory = categories.FirstOrDefault(c => c.categoryName == "Beh. app Medarbetare");
        if (staffCategory is null)
            logger.LogWarning("No Quinyx category matched 'Beh. app Medarbetare'.");

        var quinyxEmployee = MapToQuinyxEmployee(details, unit);
        
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

    // Returns the Quinyx unit API key for the employee's department, plus the department name
    // to use as extSectionId when the match was made on the parent department.
    private async Task<(string? ApiKey, string? Unit)> MapApiKeyAsync(HaileyDeatils details, CancellationToken ct)
    {
        var departments = details.HaileyCompany?.Departments ?? [];
        var departmentId = details.HaileyEmployeeDetails.OrganizationalBelonging?.DepartmentId;
        var department = departments.FirstOrDefault(x => x.Id == departmentId);
        if (string.IsNullOrEmpty(department?.Name))
        {
            logger.LogWarning("Department {DepartmentId} not found in haileyCompany.departments.", departmentId);
            return (null, null);
        }

        if (quinyxService.quinyxGroups?.Contains(department.Name, StringComparison.OrdinalIgnoreCase) != true)
        {
            return (null, null);
        }

        var units = await quinyxService.GetUnitsAPIKeyAsync(ct);
        var matchedUnit = units.FirstOrDefault(u => Fuzz.Ratio(u.name, department.Name) >= 80);
        if (matchedUnit is not null)
        {
            return (matchedUnit.API_key, null);
        }

        var parentDepartment = departments.FirstOrDefault(x => x.Id == department.BelongingToDepartmentId);
        if (string.IsNullOrEmpty(parentDepartment?.Name))
        {
            return (null, null);
        }

        matchedUnit = units.FirstOrDefault(u => Fuzz.Ratio(u.name, parentDepartment.Name) >= 70);
        return (matchedUnit?.API_key, department.Name);
    }

    private UpdateEmployee MapToQuinyxEmployee(HaileyDeatils src, string? unit)
    {
        var employee = src.HaileyEmployeeDetails;
        var sensitive = employee.Personal?.Sensitive;
        var contact = employee.Personal?.ContactInformation;
        var dest = new UpdateEmployee
        {
            badgeNo = employee.JobData.General.EmploymentNumber,
            givenName = employee.Personal?.General?.FirstName,
            familyName = employee.Personal?.General?.LastName,
            email = employee.JobData.General.CompanyEmail,

            socsecNo = sensitive?.PersonalIdentityNumber is { } pin
                ? NonAlphanumericRegex().Replace(pin, "")
                : null,
            address1 = contact?.StreetAddress,
            zip = contact?.PostalCode,
            city = contact?.City,
            country = contact?.Country,
            // Manager is optional; leave reportingTo unset when Hailey has no manager.
            reportingTo = string.IsNullOrWhiteSpace(src.HaileyManagerEmployeeNumber) ? null : src.HaileyManagerEmployeeNumber,
            active = 1,
            activeSpecified = true,
            extSectionId = unit,
        };

        if (!string.IsNullOrWhiteSpace(sensitive?.Gender))
        {
            dest.sex = sensitive.Gender.Trim().ToLower() switch
            {
                "male" => 0,
                "female" => 1,
                _ => 3
            };
            dest.sexSpecified = true;
        }

        if (sensitive?.DateOfBirth.HasValue == true)
        {
            dest.dateOfBirth = sensitive.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue);
            dest.dateOfBirthSpecified = true;
        }

        if (employee.JobData.Employment?.DateOfJoining.HasValue == true)
        {
            dest.employedDate = employee.JobData.Employment.DateOfJoining.Value.ToDateTime(TimeOnly.MinValue);
            dest.employedDateSpecified = true;
        }
        if (employee.JobData.Employment?.LastDayOfEmployment.HasValue == true)
        {
            dest.leaveDate = employee.JobData.Employment.LastDayOfEmployment.Value.ToDateTime(TimeOnly.MinValue);
            dest.leaveDateSpecified = true;
        }
        return dest;
    }

}
