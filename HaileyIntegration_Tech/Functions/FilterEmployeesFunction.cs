using System.Net;
using System.Text.Json;
using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HaileyIntegration.Tech.Functions;

public sealed class FilterEmployeesFunction(
    IEmployeeFilterService filterService,
    ILogger<FilterEmployeesFunction> logger)
{

    [Function(nameof(FilterEmployeesFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "employees/filter")] HttpRequestData req,
        CancellationToken ct)
    {
        FilterEmployeesRequest request;
        try
        {
            request = await JsonSerializer.DeserializeAsync<FilterEmployeesRequest>(
                req.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                ct)
                ?? throw new ArgumentNullException("Request body is empty.");
        }
        catch (Exception ex)
        {
            logger.LogWarning("Invalid request body: {Message}", ex.Message);
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync($"Invalid request: {ex.Message}", ct);
            return bad;
        }

        logger.LogInformation(
            "Filtering {Total} employees with a {Window}-hour window",
            request.Employees.Count, request.WindowHours);

        var result = filterService.FilterByLastUpdated(request);

        logger.LogInformation(
            "Filter result: {Filtered}/{Total} employees updated since {WindowStart:u}",
            result.FilteredCount, result.TotalReceived, result.WindowStart);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(result, ct);
        return response;
    }
}
