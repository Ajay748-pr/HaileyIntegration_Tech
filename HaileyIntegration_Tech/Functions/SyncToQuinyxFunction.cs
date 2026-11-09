using System.Net;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Quinyx;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HaileyIntegration.Tech.Functions;

public sealed class SyncToQuinyxFunction(
    QuinyxEmployeeUpdater employeeUpdater,
    QuinyxAgreementUpdater agreementUpdater,
    IConfiguration configuration,
    ILogger<SyncToQuinyxFunction> logger)
{
    [Function(nameof(SyncToQuinyxFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync/quinyx")]
        HttpRequestData req,
        CancellationToken ct)
    {
        logger.LogInformation(
            "SyncToQuinyxFunction triggered. RequestId={RequestId}", req.FunctionContext.InvocationId);

        // The body stream can only be read once, so buffer it for both deserialization and the blob copy.
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync(ct);

        HaileyDeatils? haileyDeatils;
        try
        {
            haileyDeatils = JsonSerializer.Deserialize<HaileyDeatils>(requestBody, AppJsonOptions.Inbound);
        }
        catch (JsonException ex)
        {
            logger.LogWarning("Invalid request body: {Message}", ex.Message);
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync(ex.Message, ct);
            return bad;
        }

        await SaveRequestBodyAsync(requestBody, haileyDeatils?.HaileyEmployeeDetails?.EmployeeId, ct);

        var employmentNumber = haileyDeatils?.HaileyEmployeeDetails?.JobData?.General?.EmploymentNumber;
        if (haileyDeatils?.HaileyEmployeeDetails is null || string.IsNullOrEmpty(employmentNumber))
        {
            logger.LogWarning("Request body is missing haileyEmployeeDetails or its employment number.");
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("haileyEmployeeDetails.jobData.general.employmentNumber is required.", ct);
            return bad;
        }

        logger.LogInformation(
            "Processing employee {EmploymentNumber} → UpdateEmployee then UpdateAgreement",
             employmentNumber);

        // Step 1 — UpdateEmployee
        var empResult = await employeeUpdater.ExecuteAsync(haileyDeatils, ct);

       
        if (!empResult.Success)
        {
            logger.LogWarning(
                "UpdateEmployee failed: {Message}",
                empResult.Message);

            empResult.ApiKey = null;
            return await WriteResultAsync(req, HttpStatusCode.UnprocessableEntity, new
            {
                updateEmployee  = empResult,
                updateAgreement = (object?)null
            });
        }

        // Step 2 — UpdateAgreement
       
        var agreeResult = await agreementUpdater.ExecuteAsync(haileyDeatils, empResult.ApiKey!, ct);

        var status = agreeResult.Success ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity;
        empResult.ApiKey = null;
        return await WriteResultAsync(req, status, new
        {
            updateEmployee  = empResult,
            updateAgreement = agreeResult
        });
    }

    // Archives the raw request as <employeeId>_<yyyyMMddHHmmss>.txt. A storage failure is
    // logged but must not stop the Quinyx sync.
    private async Task SaveRequestBodyAsync(string requestBody, string? employeeId, CancellationToken ct)
    {
        var connectionString = configuration["BlobStorageConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("BlobStorageConnectionString is not configured. Request body not saved.");
            return;
        }

        var containerName = configuration["BlobContainerName"] ?? "hailey-requests";
        var blobName = $"{employeeId ?? "unknown"}_{DateTime.UtcNow:yyyyMMddHHmmss}.txt";

        try
        {
            var container = new BlobContainerClient(connectionString, containerName);
            await container.CreateIfNotExistsAsync(cancellationToken: ct);

            await container.GetBlobClient(blobName).UploadAsync(
                BinaryData.FromString(requestBody),
                new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "text/plain" } },
                ct);

            logger.LogInformation("Saved request body to blob {Container}/{BlobName}", containerName, blobName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to save request body to blob {Container}/{BlobName}", containerName, blobName);
        }
    }

    // The Quinyx updates have already run by this point, so a caller that has
    // disconnected must not turn a completed sync into a failed invocation.
    private async Task<HttpResponseData> WriteResultAsync(HttpRequestData req, HttpStatusCode status, object body)
    {
        var response = req.CreateResponse(status);
        try
        {
            await response.WriteAsJsonAsync(body, CancellationToken.None);
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException)
        {
            logger.LogWarning(
                "Caller disconnected before the response was written. Sync completed with Status={Status}. RequestId={RequestId}",
                (int)status, req.FunctionContext.InvocationId);
        }
        return response;
    }
}
