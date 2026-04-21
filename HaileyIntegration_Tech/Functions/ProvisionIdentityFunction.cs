using System.Net;
using System.Text.Json;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Services;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HaileyIntegration.Tech.Functions;

public sealed class ProvisionIdentityFunction(
    IIdentityProvisioningService identityService,
    IEmployeeMappingService mappingService,
    ILogger<ProvisionIdentityFunction> logger)
{
    [Function(nameof(ProvisionIdentityFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "identity/provision")] HttpRequestData req,
        CancellationToken ct)
    {
        HaileyEmployee? employee;
        try
        {
            employee = await JsonSerializer.DeserializeAsync<HaileyEmployee>(
                req.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Invalid identity provision request: {Message}", ex.Message);
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync(ex.Message, ct);
            return bad;
        }

        if (employee is null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Employee payload is required.", ct);
            return bad;
        }

        var isTermination = IsTerminated(employee.AccountStatus, employee.EmploymentStatus);
        var changeType = isTermination ? ChangeType.Termination : ChangeType.Update;
        var canonical = mappingService.MapToCanonical(employee, changeType);

        logger.LogInformation(
            "Identity {Action} for employee {EmployeeNumber}",
            isTermination ? "deprovision" : "provision",
            employee.EmploymentNumber);

        var result = isTermination
            ? await identityService.DeprovisionAsync(canonical, ct)
            : await identityService.ProvisionAsync(canonical, ct);

        var status = result.Success ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity;
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(result, ct);
        return response;
    }

    private static bool IsTerminated(string? accountStatus, string? employmentStatus)
    {
        var status = (employmentStatus ?? accountStatus ?? string.Empty).ToLowerInvariant();
        return status is "terminated" or "resigned" or "retired" or "inactive";
    }
}
