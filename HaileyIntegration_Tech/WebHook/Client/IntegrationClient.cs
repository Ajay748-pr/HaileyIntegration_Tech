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
        response.EnsureSuccessStatusCode();

    }

    public async Task SendUpdateEmployeeAsync(string employeeId)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, _configuration["NewEmployeeApiUrl"]);

        var jsonToSend = "{\"employeeId\": " + employeeId + " }";
        var content = new StringContent(jsonToSend, null, "application/json");
        request.Content = content;


        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task SendDeactivateEmployeeAsync(string employeeData, string companyData)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, _configuration["DeactivateEmployeeApiUrl"]);

        var jsonToSend = "{\"haileyEmployee\": " + employeeData + ",\r\n  \"haileyCompany\": " + companyData + "\r\n }";

        var content = new StringContent(jsonToSend, null, "application/json");
        request.Content = content;


        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
