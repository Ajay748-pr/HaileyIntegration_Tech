using System.Net.Http.Headers;
using System.Net.Http.Json;
using HaileyIntegration.Tech.Models.Dto;
using Microsoft.Extensions.Logging;

namespace HaileyIntegration.Tech.Services.Downstream;

public sealed class VismaService(HttpClient http, VismaOptions options, ILogger<VismaService> logger) : IVismaService
{
    public async Task<VismaResult> SyncDimensionAsync(VismaDimensionRequest request, CancellationToken ct = default)
    {
        var url = $"v1/dimension/{request.DimensionId}/{request.SegementId}";

        logger.LogInformation(
            "Calling Visma PUT {Url} for dimensionId={DimensionId} segmentId={SegmentId}",
            url, request.DimensionId, request.SegementId);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = JsonContent.Create(request, options: AppJsonOptions.Outbound),
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", options.BearerToken) }
        };

        try
        {
            var response = await http.SendAsync(httpRequest, ct);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Visma sync succeeded for dimensionId={DimensionId} segmentId={SegmentId}",
                    request.DimensionId, request.SegementId);

                return new VismaResult { Success = true, Message = "Dimension synced successfully." };
            }

            var error = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning(
                "Visma sync failed for dimensionId={DimensionId} segmentId={SegmentId}: {Status} {Error}",
                request.DimensionId, request.SegementId, response.StatusCode, error);

            return new VismaResult
            {
                Success = false,
                ErrorCode = ((int)response.StatusCode).ToString(),
                Message = error
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Visma sync threw for dimensionId={DimensionId} segmentId={SegmentId}",
                request.DimensionId, request.SegementId);

            return new VismaResult
            {
                Success = false,
                ErrorCode = "EXCEPTION",
                Message = ex.Message
            };
        }
    }
}
