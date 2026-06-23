using System.Text.RegularExpressions;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Extensions.Logging;
using ServiceReference1;

namespace HaileyIntegration.Tech.Quinyx;

public sealed partial class QuinyxEmployeeUpdater(
    IQuinyxService quinyxService,
    ILogger<QuinyxEmployeeUpdater> logger)
{
    [GeneratedRegex(@"[^0-9A-Za-z]")]
    private static partial Regex NonAlphanumericRegex();
    public async Task<SyncResult> ExecuteAsync(HaileyDeatils details, CancellationToken ct = default)
    {
        logger.LogInformation(
            "UpdateEmployee starting for EmploymentNumber={EmploymentNumber}", details.HaileyEmployeeDetails.JobData.General.EmploymentNumber);

        var quinyxEmployee = MapToQuinyxEmployee(details);
        var result = await quinyxService.UpdateEmployeeAsync(quinyxEmployee, ct);

        logger.LogInformation(
            "UpdateEmployee completed. Success={Success} EmployeeNumber={EmployeeNumber} Message={Message}",
            result.Success, result.EmployeeNumber, result.Message);

        return result;
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
            extSectionId = src.HaileyCompany.Departments.FirstOrDefault(x => x.Id == src.HaileyEmployee.DepartmentId).Name,


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
