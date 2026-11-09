using Azure;
using HaileyWebhook.Client;
using HaileyWebhook.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HaileyWebhook;

public class HaileyWebhook
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<HaileyWebhook> _logger;
    private readonly HaileyClient _haileyClient;
    private readonly IntegrationClient _integrationClient;

    public HaileyWebhook(
        ILogger<HaileyWebhook> logger,
        HaileyClient haileyClient,
        IntegrationClient quninyxClient)
    {
        _logger = logger;
        _haileyClient = haileyClient;
        _integrationClient = quninyxClient;
    }

    [Function("HaileyWebhook")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        _logger.LogInformation("Request Body: {Body}", body);
        if (string.IsNullOrWhiteSpace(body))
        {
            return new BadRequestObjectResult("Request body is empty.");
        }

        RequestPayload? requestPayload;
        try
        {
            requestPayload = JsonSerializer.Deserialize<RequestPayload>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Unable to parse inbound request body.");
            return new BadRequestObjectResult("Invalid JSON request payload.");
        }

        if (requestPayload is null || string.IsNullOrWhiteSpace(requestPayload.EmployeeId))
        {
            return new BadRequestObjectResult("EmployeeId is required.");
        }

        try
        {
            var (caller, employeeData, employeeId, departmentId, mangerId) = await _haileyClient.GetEmployeeAsync(requestPayload.EmployeeId);
            
            if (caller == "newInformation")
            {
                mangerId = await GetManagerEmployeeNumberAsync(mangerId, employeeId);

                var message = "new information request sent for " + employeeId;
                _logger.LogInformation(message);
                var companyData = await _haileyClient.GetCompanyAsync();
                await _integrationClient.SendUpdateEmployeeAsync(employeeData, companyData, mangerId);
                return new OkObjectResult(new
                {
                    message,
                });
            }
            else if (caller == "newEmployee")
            {
                mangerId = await GetManagerEmployeeNumberAsync(mangerId, employeeId);
                var message = "new information request sent for " + employeeId;
                _logger.LogInformation(message);
                var companyData = await _haileyClient.GetCompanyAsync();
                _integrationClient.SendUpdateEmployeeAsync(employeeData, companyData, mangerId);
                var companyList = companyData is not null
                    ? JsonSerializer.Deserialize<HaileyCompany>(companyData) : null;
                var logicAppInput = employeeData.TrimStart(new[] { '{' });
                _integrationClient.SendLogicAppRequest("{\"departmentName\":\"" + companyList.Departments.FirstOrDefault(x => x.Id == departmentId).Name + "\"," + logicAppInput);
                return new OkObjectResult(new
                {
                    message,
                });
            }
            else if (caller == "inactiveEmployee")
            {
                var message = "inactive user.";
                _logger.LogInformation(message);
                return new OkObjectResult(new
                {
                    message,
                });
            }

            _logger.LogInformation("no action.");
            return new OkObjectResult(new
            {
                message = "no action.",
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Configuration or payload issue while processing webhook.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "External API call failed.");
            return new ObjectResult("External API call failed.") { StatusCode = StatusCodes.Status502BadGateway };
        }
    }

    // The manager is optional: a missing or unresolvable manager must not stop the employee sync.
    private async Task<string?> GetManagerEmployeeNumberAsync(string? managerId, string? employeeId)
    {
        if (string.IsNullOrEmpty(managerId))
        {
            _logger.LogWarning("Employee {EmployeeId} has no manager. Syncing without manager.", employeeId);
            return null;
        }

        try
        {
            return await _haileyClient.GetMangerEmployeeNumberAsync(managerId);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Could not resolve manager {ManagerId} for employee {EmployeeId}. Syncing without manager.", managerId, employeeId);
            return null;
        }
    }
}

public sealed class RequestPayload
{
    public string? CompanyId { get; init; }
    public string? EmployeeId { get; init; }
    public organizationalBelonging? organizationalBelonging { get; init; }


}

public sealed class EmployeeResponse
{
    public string? EmployeeId { get; init; }
    public List<Salary>? Salaries { get; init; }
}

public sealed class Salary
{
    public List<SalaryHistoryItem>? History { get; init; }
}
public sealed class organizationalBelonging
{
    public string managerEmployeeId { get; init; }
}

public sealed class SalaryHistoryItem
{
    public string? Date { get; init; }
    public decimal? Amount { get; init; }
}