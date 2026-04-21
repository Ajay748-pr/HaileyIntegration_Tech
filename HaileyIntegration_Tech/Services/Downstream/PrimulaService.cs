using System.Net.Http.Json;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using Microsoft.Extensions.Logging;

namespace HaileyIntegration.Tech.Services.Downstream;

public sealed class PrimulaService(HttpClient http, ILogger<PrimulaService> logger) : IPrimulaService
{
    public async Task<SyncResult> SyncEmployeeAsync(CanonicalEmployee employee, CancellationToken ct = default)
    {
        var payload = new
        {
            employeeNumber = employee.EmployeeNumber,
            firstName = employee.FirstName,
            lastName = employee.LastName,
            employmentType = employee.EmploymentType,
            employmentStatus = employee.EmploymentStatus,
            startDate = employee.StartDate?.ToString("yyyy-MM-dd"),
            endDate = employee.EndDate?.ToString("yyyy-MM-dd"),
            lastWorkingDay = employee.LastWorkingDay?.ToString("yyyy-MM-dd"),
            scopePercentage = employee.ScopePercentage,
            scopeHours = employee.ScopeHours,
            vacationDays = employee.VacationDays,
            fixedTermType = employee.FixedTermType,
            endOfFixedTerm = employee.EndOfFixedTerm?.ToString("yyyy-MM-dd"),
            costCenterId = employee.CostCenterId,
            bankName = employee.BankName,
            clearingNumber = employee.ClearingNumber,
            accountNumber = employee.AccountNumber,
            iban = employee.Iban,
            bic = employee.Bic
        };

        try
        {
            var response = await http.PostAsJsonAsync("employees", payload, ct);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Primula sync succeeded for employee {EmployeeNumber}", employee.EmployeeNumber);
                return new SyncResult { Success = true, EmployeeNumber = employee.EmployeeNumber, TargetSystem = "Primula" };
            }

            var error = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Primula sync failed for {EmployeeNumber}: {Status} {Error}",
                employee.EmployeeNumber, response.StatusCode, error);

            return new SyncResult
            {
                Success = false,
                EmployeeNumber = employee.EmployeeNumber,
                TargetSystem = "Primula",
                ErrorCode = ((int)response.StatusCode).ToString(),
                Message = error
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Primula sync threw for employee {EmployeeNumber}", employee.EmployeeNumber);
            return new SyncResult
            {
                Success = false,
                EmployeeNumber = employee.EmployeeNumber,
                TargetSystem = "Primula",
                ErrorCode = "EXCEPTION",
                Message = ex.Message
            };
        }
    }
}
