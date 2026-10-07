using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
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

        try
        {
            var accessToken = await GetAccessTokenAsync(ct);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = JsonContent.Create(request, options: AppJsonOptions.Outbound),
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
            };

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

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, options.TokenUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = options.ClientId,
                ["client_secret"] = options.ClientSecret,
                ["scope"] = options.Scope,
                ["tenant_id"] = options.TenantId
            }),
            Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/json") } }
        };

        var response = await http.SendAsync(tokenRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Visma token request failed: {Status} {Error}", response.StatusCode, error);
            throw new HttpRequestException($"Visma token request failed: {(int)response.StatusCode} {error}");
        }

        var token = await response.Content.ReadFromJsonAsync<VismaTokenResponse>(ct);

        return string.IsNullOrWhiteSpace(token?.AccessToken)
            ? throw new InvalidOperationException("Visma token response did not contain an access_token.")
            : token.AccessToken;
    }

    private sealed record VismaTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken);
}
