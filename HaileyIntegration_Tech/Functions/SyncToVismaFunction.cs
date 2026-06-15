using System.Net;
using System.Text.Json;
using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HaileyIntegration.Tech.Functions;

public sealed class SyncToVismaFunction(
    IVismaService vismaService,
    ILogger<SyncToVismaFunction> logger)
{
    [Function(nameof(SyncToVismaFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync/visma")]
        HttpRequestData req,
        CancellationToken ct)
    {
        logger.LogInformation(
            "SyncToVismaFunction triggered. RequestId={RequestId}", req.FunctionContext.InvocationId);

        VismaDimensionRequest? dimensionRequest;
        try
        {
            dimensionRequest = await JsonSerializer.DeserializeAsync<VismaDimensionRequest>(
                req.Body, AppJsonOptions.Inbound, ct);
        }
        catch (JsonException ex)
        {
            logger.LogWarning("Invalid request body: {Message}", ex.Message);
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync(ex.Message, ct);
            return bad;
        }

        if (dimensionRequest is null || string.IsNullOrWhiteSpace(dimensionRequest.DimensionId))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("A valid payload with dimensionId is required.", ct);
            return bad;
        }

        logger.LogInformation(
            "Syncing dimension {DimensionId} segmentId={SegmentId} to Visma",
            dimensionRequest.DimensionId, dimensionRequest.SegementId);

        var result = await vismaService.SyncDimensionAsync(dimensionRequest, ct);

        var status = result.Success ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity;
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(result, ct);
        return response;
    }
}
