using System.Net.Http.Json;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using Microsoft.Extensions.Logging;

namespace HaileyIntegration.Tech.Services.Downstream;

public sealed class LearnifyService(HttpClient http, ILogger<LearnifyService> logger) : ILearnifyService
{
    public async Task<SyncResult> SyncEmployeeAsync(CanonicalEmployee employee, CancellationToken ct = default)
    {
        var payload = new
        {
            employeeNumber = employee.EmployeeNumber,
            firstName = employee.FirstName,
            lastName = employee.LastName,
            email = employee.CompanyEmail,
            departmentId = employee.DepartmentId,
            employmentStatus = employee.EmploymentStatus,
            startDate = employee.StartDate?.ToString("yyyy-MM-dd")
        };

        try
        {
            var response = await http.PostAsJsonAsync("users/sync", payload, ct);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Learnify sync succeeded for employee {EmployeeNumber}", employee.EmployeeNumber);
                return new SyncResult { Success = true, EmployeeNumber = employee.EmployeeNumber, TargetSystem = "Learnify" };
            }

            var error = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Learnify sync failed for {EmployeeNumber}: {Status} {Error}",
                employee.EmployeeNumber, response.StatusCode, error);

            return new SyncResult
            {
                Success = false,
                EmployeeNumber = employee.EmployeeNumber,
                TargetSystem = "Learnify",
                ErrorCode = ((int)response.StatusCode).ToString(),
                Message = error
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Learnify sync threw for employee {EmployeeNumber}", employee.EmployeeNumber);
            return new SyncResult
            {
                Success = false,
                EmployeeNumber = employee.EmployeeNumber,
                TargetSystem = "Learnify",
                ErrorCode = "EXCEPTION",
                Message = ex.Message
            };
        }
    }
}
