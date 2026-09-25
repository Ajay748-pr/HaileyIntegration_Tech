using HaileyWebhook.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HaileyWebhook;

public class DeactivateUser
{
    private readonly ILogger<DeactivateUser> _logger;
    private readonly HaileyClient _haileyClient;
    private readonly IntegrationClient _integrationClient;

    public DeactivateUser(ILogger<DeactivateUser> logger, HaileyClient haileyClient, IntegrationClient integrationClient)
    {
        _logger = logger;
        _haileyClient = haileyClient;
        _integrationClient = integrationClient;
    }

    [Function("DeactivateUser")]
    public async Task Run([TimerTrigger("0 0 21,22 * * *")] TimerInfo _)
    {
        _logger.LogInformation("DeactivateUser timer trigger executed at: {executionTime}", DateTime.Now);

        var employees = await _haileyClient.GetEmployeesAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var twoDaysAgo = today.AddDays(-2);

        var inactiveEmployees = employees
            .Where(e => e.LastDayOfEmployment.HasValue &&
                        e.LastDayOfEmployment.Value >= twoDaysAgo &&
                        e.LastDayOfEmployment.Value <= today)
            .ToList();
        _logger.LogInformation("Found {count} inactive employee(s) to deactivate.", inactiveEmployees.Count);

        foreach (var employee in inactiveEmployees)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(employee);
                var companyData = await _haileyClient.GetCompanyAsync();
                _integrationClient.SendDeactivateEmployeeAsync(jsonString, companyData);
                _logger.LogInformation("Deactivated employee {employeeId}.", employee.EmployeeId);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to deactivate employee {employeeId}.", employee.EmployeeId);
            }
        }
    }
}
