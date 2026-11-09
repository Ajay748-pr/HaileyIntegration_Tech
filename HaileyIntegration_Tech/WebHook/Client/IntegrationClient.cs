using Microsoft.Extensions.Configuration;

namespace HaileyWebhook.Client;

public sealed class IntegrationClient
{
    private readonly IConfiguration _configuration;

    public IntegrationClient(HttpClient httpClient, IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendAgreementAsync(string employeeData, string CompanyData)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, _configuration["QuninyxAgreementApiUrl"]);

        var jsonToSend = "{\"haileyEmployeeDetails\": " + employeeData + ",\r\n  \"haileyCompany\": " + CompanyData + "\r\n }";
        var content = new StringContent(jsonToSend, null, "application/json");
        request.Content = content;

        var quninyxApiKey = _configuration["QuninyxAgreementApiKey"];
        if (!string.IsNullOrWhiteSpace(quninyxApiKey))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", quninyxApiKey);
        }

        var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response);

    }

    public async Task SendUpdateEmployeeAsync(string employeeData, string CompanyData, string? Manager)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, _configuration["NewEmployeeApiUrl"]);

        // Manager is optional: send JSON null instead of an empty string when it is missing.
        var managerJson = string.IsNullOrWhiteSpace(Manager) ? "null" : System.Text.Json.JsonSerializer.Serialize(Manager);
        var jsonToSend = "{\"haileyEmployeeDetails\": " + employeeData + ",\r\n  \"haileyCompany\": " + CompanyData + ",\r\n  \"haileyManagerEmployeeNumber\": " + managerJson + "}";
        var content = new StringContent(jsonToSend, null, "application/json");
        request.Content = content;


        var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response);
    }

    public async Task SendLogicAppRequest(string employeeData)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, _configuration["NewEmployeeLogicAppUrl"]+ employeeData);

        var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response);
    }

    public async Task SendDeactivateEmployeeAsync(string employeeData, string companyData)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, _configuration["DeactivateEmployeeApiUrl"]);

        var jsonToSend = "{\"haileyEmployee\": " + employeeData + ",\r\n  \"haileyCompany\": " + companyData + "\r\n }";

        var content = new StringContent(jsonToSend, null, "application/json");
        request.Content = content;


        var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response);
    }

    // Like EnsureSuccessStatusCode, but keeps the response body so the downstream failure reason is logged.
    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException(
            $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {body}",
            null,
            response.StatusCode);
    }
}
