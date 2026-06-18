using System.Text.Json;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Extensions.Logging;
using ServiceReference1;

namespace HaileyIntegration.Tech.Quinyx;

public sealed class QuinyxEmployeeUpdater(
    IQuinyxService quinyxService,
    ILogger<QuinyxEmployeeUpdater> logger)
{
    public async Task<SyncResult> ExecuteAsync(HaileyEmployee employee, CancellationToken ct = default)
    {
        logger.LogInformation(
            "UpdateEmployee starting for EmploymentNumber={EmploymentNumber}", employee.EmploymentNumber);

        var quinyxEmployee = MapToQuinyxEmployee(employee);
        var result = await quinyxService.UpdateEmployeeAsync(quinyxEmployee, ct);

        logger.LogInformation(
            "UpdateEmployee completed. Success={Success} EmployeeNumber={EmployeeNumber} Message={Message}",
            result.Success, result.EmployeeNumber, result.Message);

        return result;
    }

    private UpdateEmployee MapToQuinyxEmployee(HaileyEmployee src)
    {
        var dest = new UpdateEmployee
        {
            badgeNo       = src.EmploymentNumber,
            givenName     = src.FirstName,
            familyName    = src.LastName,
            email         = src.CompanyEmail,
            phoneNo       = src.WorkPhone,
            cellPhone     = src.PrivatePhone,
            socsecNo      = src.PersonalIdentityNumber,
            address1      = src.StreetAddress,
            zip           = src.PostalCode,
            city          = src.City,
            country       = src.Country,
            reportingTo   = src.ReportingTo,
            extCostCentre = src.ExtCostCentre,
            active = 1,
            activeSpecified = true,
        };

        if (!string.IsNullOrWhiteSpace(src.DateOfBirth) &&
            DateTime.TryParse(src.DateOfBirth, out var dob))
        {
            dest.dateOfBirth          = dob;
            dest.dateOfBirthSpecified = true;
        }

        if (src.DateOfJoining.HasValue)
        {
            dest.employedDate          = src.DateOfJoining.Value.ToDateTime(TimeOnly.MinValue);
            dest.employedDateSpecified = true;
        }

        if (src.LastDayOfEmployment.HasValue)
        {
            dest.leaveDate          = src.LastDayOfEmployment.Value.ToDateTime(TimeOnly.MinValue);
            dest.leaveDateSpecified = true;
        }

        return dest;
    }
}
