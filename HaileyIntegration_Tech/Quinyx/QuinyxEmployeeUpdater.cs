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
        if (string.IsNullOrEmpty(details.HaileyEmployeeDetails.JobData.General.EmploymentNumber)
            || string.IsNullOrEmpty(details.HaileyEmployeeDetails.JobData.General.CompanyEmail)
            || string.IsNullOrEmpty(details.HaileyMangerEmployeeNumber)
            || string.IsNullOrEmpty(details.HaileyEmployeeDetails.JobData.Employment.Employments[0].OrganizationalInformation.DepartmentId)
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

        if (string.IsNullOrEmpty(apiKey))
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
        var department = details.HaileyCompany.Departments.FirstOrDefault(x => x.Id == details.HaileyEmployeeDetails.JobData.Employment.Employments[0].OrganizationalInformation.DepartmentId);
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

    private UpdateEmployee MapToQuinyxEmployee(HaileyDeatils src)
    {
        var dest = new UpdateEmployee
        {
            badgeNo = src.HaileyEmployeeDetails.JobData.General.EmploymentNumber,
            givenName = src.HaileyEmployeeDetails.Personal.General.FirstName,
            familyName = src.HaileyEmployeeDetails.Personal.General.LastName,
            email = src.HaileyEmployeeDetails.JobData.General.CompanyEmail,
           
            socsecNo = src.HaileyEmployeeDetails.Personal.Sensitive.PersonalIdentityNumber is { } pin
                ? NonAlphanumericRegex().Replace(pin, "")
                : null,
            address1 = src.HaileyEmployeeDetails.Personal.ContactInformation.StreetAddress,
            zip = src.HaileyEmployeeDetails.Personal.ContactInformation.PostalCode,
            city = src.HaileyEmployeeDetails.Personal.ContactInformation.City,
            country = src.HaileyEmployeeDetails.Personal.ContactInformation.Country,
            reportingTo = src.HaileyMangerEmployeeNumber,
            active = 1,
            activeSpecified = true,

        };

        if (!string.IsNullOrWhiteSpace(src.HaileyEmployeeDetails.Personal.Sensitive.Gender))
        {
            dest.sex = src.HaileyEmployeeDetails.Personal.Sensitive.Gender?.Trim().ToLower() switch
            {
                "male" => 0,
                "female" => 1,
                _ => 3
            };
            dest.sexSpecified = true;
        }

        if (src.HaileyEmployeeDetails.Personal.Sensitive.DateOfBirth.HasValue)
        {
            dest.dateOfBirth = src.HaileyEmployeeDetails.Personal.Sensitive.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue); ;
            dest.dateOfBirthSpecified = true;
        }

        if (src.HaileyEmployeeDetails.JobData.Employment.DateOfJoining.HasValue)
        {
            dest.employedDate = src.HaileyEmployeeDetails.JobData.Employment.DateOfJoining.Value.ToDateTime(TimeOnly.MinValue);
            dest.employedDateSpecified = true;
        }
        if (src.HaileyEmployeeDetails.JobData.Employment.LastDayOfEmployment.HasValue)
        {
            dest.leaveDate = src.HaileyEmployeeDetails.JobData.Employment.LastDayOfEmployment.Value.ToDateTime(TimeOnly.MinValue);
            dest.leaveDateSpecified = true;
        }
        return dest;
    }

}
