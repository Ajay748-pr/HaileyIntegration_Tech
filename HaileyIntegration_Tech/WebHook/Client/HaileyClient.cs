using HaileyWebhook.Model;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace HaileyWebhook.Client;

public sealed class HaileyClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public HaileyClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<(string, string,string)> GetEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        var employeeApiUrl = _configuration["HaileyEmployeeApiUrl"] ?? _configuration["EmployeeApiUrl"];
        if (string.IsNullOrWhiteSpace(employeeApiUrl))
        {
            throw new InvalidOperationException("Employee API URL is not configured.");
        }

        var employeeUrl = employeeApiUrl.Replace("{employeeId}", Uri.EscapeDataString(employeeId));
        using var request = new HttpRequestMessage(HttpMethod.Get, employeeUrl);

        var haileyApiKey = _configuration["HaileyApiKey"];
        if (!string.IsNullOrWhiteSpace(haileyApiKey))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", haileyApiKey);
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Employee API call failed with status {(int)response.StatusCode}: {responseBody}");
        }

        try
        {
            var haileyDeatils = await JsonSerializer.DeserializeAsync<HaileyEmployeeDetails>(await response.Content.ReadAsStreamAsync());
            var caller = callIntegration(haileyDeatils);
            return (caller, responseBody, haileyDeatils?.EmployeeId);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Unable to parse employee response payload.", ex);
        }
    }

    private string callIntegration(HaileyEmployeeDetails haileyDeatils)
    {
        if (string.IsNullOrEmpty(haileyDeatils?.JobData?.General?.CompanyEmail)
            || string.IsNullOrEmpty(haileyDeatils.JobData.General.EmploymentNumber)
            || haileyDeatils?.JobData?.Employment?.Employments is null
            || string.IsNullOrEmpty(haileyDeatils?.JobData?.Employment?.Employments[0].OrganizationalInformation.ManagerEmployeeId)
            || string.IsNullOrEmpty(haileyDeatils?.JobData?.Employment?.Employments[0].OrganizationalInformation.DepartmentId)
            )
            return null;

        if (haileyDeatils?.JobData?.Employment?.LastDayOfEmployment < DateOnly.FromDateTime(DateTime.Today))//employee left
            return "inactiveEmployee";

        if (haileyDeatils?.JobData?.Employment?.DateOfJoining >= DateOnly.FromDateTime(DateTime.Today))//New employee
            return "newEmployee";

        if (haileyDeatils?.Salaries.FirstOrDefault().History.Where(h => h.Date.HasValue).MaxBy(h => h.Date!.Value).Date >= DateOnly.FromDateTime(DateTime.Today))//New agreement
            return "newSalary";


        return null;
    }

    public async Task<string> GetCompanyAsync(CancellationToken cancellationToken = default)
    {
        var companyApiUrl = _configuration["HaileyCompanyApiUrl"] ?? _configuration["CompanyApiUrl"];
        if (string.IsNullOrWhiteSpace(companyApiUrl))
        {
            throw new InvalidOperationException("Company API URL is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, companyApiUrl);

        var haileyApiKey = _configuration["HaileyApiKey"];
        if (!string.IsNullOrWhiteSpace(haileyApiKey))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", haileyApiKey);
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Company API call failed with status {(int)response.StatusCode}: {responseBody}");
        }

        try
        {
            return responseBody;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Unable to parse employee response payload.", ex);
        }
    }



    public async Task<List<HaileyEmployee>> GetEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var allEmployeeApiUrl = _configuration["HaileyAllEmployeeApiUrl"];
        if (string.IsNullOrWhiteSpace(allEmployeeApiUrl))
        {
            throw new InvalidOperationException("All Employee API URL is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, allEmployeeApiUrl);

        var haileyApiKey = _configuration["HaileyApiKey"];
        if (!string.IsNullOrWhiteSpace(haileyApiKey))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", haileyApiKey);
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Employees API call failed with status {(int)response.StatusCode}: {responseBody}");
        }

        try
        {
            var employees = JsonSerializer.Deserialize<List<HaileyEmployee>>(responseBody, JsonOptions);
            return employees?.Where(x=>x.LastDayOfEmployment !=null).ToList() ?? [];
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Unable to parse employees response payload.", ex);
        }
    }
}
