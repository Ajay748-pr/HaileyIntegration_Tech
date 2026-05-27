using System.Net.Http.Json;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using Microsoft.Extensions.Logging;
using ServiceReference1;

namespace HaileyIntegration.Tech.Services.Downstream;

public sealed class QuinyxService(HttpClient http, QuinyxOptions options, ILogger<QuinyxService> logger) : IQuinyxService
{

    public async Task<IReadOnlyList<QuinyxRestaurant>> GetRestaurantsAsync(string changedSince, CancellationToken ct = default)
    {
        var client = new FlexForcePortTypeClient();
        try
        {
            var response = await client.wsdlGetRestaurantsAsync(options.ApiKey, changedSince);
            var restaurants = response.@return;

            await client.CloseAsync();

            if (restaurants == null || restaurants.Length == 0)
            {
                logger.LogInformation("Quinyx returned no restaurants for changedSince={ChangedSince}", changedSince);
                return [];
            }

            logger.LogInformation("Quinyx returned {Count} restaurant(s)", restaurants.Length);

            return restaurants
                .Select(r => new QuinyxRestaurant
                {
                    Id = r.id.ToString(),
                    Name = r.name,
                    ManagerId = r.managerId.ToString(),
                    Currency = r.currency
                })
                .ToList();
        }
        catch (Exception ex)
        {
            client.Abort();
            logger.LogError(ex, "Quinyx GetRestaurants threw for changedSince={ChangedSince}", changedSince);
            throw;
        }
    }

    public async Task<SyncResult> SyncEmployeeAsync(CanonicalEmployee employee, CancellationToken ct = default)
    {
        // Quinyx requires employee number as the matching key (originating from Hailey)
        var payload = new
        {
            employeeNumber = employee.EmployeeNumber,
            firstName = employee.FirstName,
            lastName = employee.LastName,
            employmentType = employee.EmploymentType,
            employmentStatus = employee.EmploymentStatus,
            startDate = employee.StartDate?.ToString("yyyy-MM-dd"),
            endDate = employee.EndDate?.ToString("yyyy-MM-dd"),
            scopePercentage = employee.ScopePercentage,
            departmentId = employee.DepartmentId,
            locationId = employee.LocationId,
            teamIds = employee.TeamIds,
            titleIds = employee.TitleIds
        };

        try
        {
            var response = await http.PostAsJsonAsync("employees", payload, ct);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Quinyx sync succeeded for employee {EmployeeNumber}", employee.EmployeeNumber);
                return new SyncResult { Success = true, EmployeeNumber = employee.EmployeeNumber, TargetSystem = "Quinyx" };
            }

            var error = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Quinyx sync failed for {EmployeeNumber}: {Status} {Error}",
                employee.EmployeeNumber, response.StatusCode, error);

            return new SyncResult
            {
                Success = false,
                EmployeeNumber = employee.EmployeeNumber,
                TargetSystem = "Quinyx",
                ErrorCode = ((int)response.StatusCode).ToString(),
                Message = error
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Quinyx sync threw for employee {EmployeeNumber}", employee.EmployeeNumber);
            return new SyncResult
            {
                Success = false,
                EmployeeNumber = employee.EmployeeNumber,
                TargetSystem = "Quinyx",
                ErrorCode = "EXCEPTION",
                Message = ex.Message
            };
        }
    }

    // ─── UpdateEmployee ───────────────────────────────────────────────────────

    public async Task<SyncResult> UpdateEmployeeAsync(UpdateEmployee employee, CancellationToken ct = default)
    {
        logger.LogInformation(
            "UpdateEmployeeAsync starting for badgeNo={BadgeNo}", employee.badgeNo);

        var result = await TriggerUpdateEmployeeSoapAsync(employee, ct);

        logger.LogInformation(
            "UpdateEmployeeAsync finished for badgeNo={BadgeNo} Success={Success}",
            employee.badgeNo, result.Success);

        return result;
    }

    // Trigger: creates the SOAP client, calls wsdlUpdateEmployees, and returns a SyncResult.
    // This is kept separate so it can be tested or retried independently.
    private async Task<SyncResult> TriggerUpdateEmployeeSoapAsync(UpdateEmployee employee, CancellationToken ct)
    {
        var client = new FlexForcePortTypeClient();
        try
        {
            logger.LogInformation(
                "Triggering SOAP wsdlUpdateEmployees for badgeNo={BadgeNo} givenName={GivenName} familyName={FamilyName} email={Email} phoneNo={PhoneNo} employedDate={EmployedDate} leaveDate={LeaveDate}",
                employee.badgeNo, employee.givenName, employee.familyName, employee.email,
                employee.phoneNo,
                employee.employedDateSpecified ? employee.employedDate.ToString("yyyy-MM-dd") : "(not set)",
                employee.leaveDateSpecified    ? employee.leaveDate.ToString("yyyy-MM-dd")    : "(not set)");

            var response = await client.wsdlUpdateEmployeesAsync(options.ApiKey, [employee]);

            await client.CloseAsync();

            var returned = response.@return;

            if (returned is null || returned.Length == 0)
            {
                logger.LogWarning(
                    "Quinyx wsdlUpdateEmployees returned empty response for badgeNo={BadgeNo}", employee.badgeNo);

                return new SyncResult
                {
                    Success        = false,
                    EmployeeNumber = employee.badgeNo,
                    TargetSystem   = "Quinyx",
                    ErrorCode      = "EMPTY_RESPONSE",
                    Message        = "Quinyx returned no employee records in the response."
                };
            }

            logger.LogInformation(
                "Quinyx wsdlUpdateEmployees succeeded for badgeNo={BadgeNo}. Records returned={Count}",
                employee.badgeNo, returned.Length);

            return new SyncResult
            {
                Success        = true,
                EmployeeNumber = employee.badgeNo,
                TargetSystem   = "Quinyx",
                Message        = $"Updated successfully. Records in response: {returned.Length}"
            };
        }
        catch (Exception ex)
        {
            client.Abort();
            logger.LogError(
                ex,
                "SOAP wsdlUpdateEmployees threw for badgeNo={BadgeNo}: {Message}",
                employee.badgeNo, ex.Message);

            return new SyncResult
            {
                Success        = false,
                EmployeeNumber = employee.badgeNo,
                TargetSystem   = "Quinyx",
                ErrorCode      = "EXCEPTION",
                Message        = ex.Message
            };
        }
    }
}
