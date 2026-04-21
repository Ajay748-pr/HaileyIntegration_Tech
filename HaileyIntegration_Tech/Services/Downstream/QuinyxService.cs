using System.Net.Http.Json;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using Microsoft.Extensions.Logging;

namespace HaileyIntegration.Tech.Services.Downstream;

public sealed class QuinyxService(HttpClient http, ILogger<QuinyxService> logger) : IQuinyxService
{
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
}
