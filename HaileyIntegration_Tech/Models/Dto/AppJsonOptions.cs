using System.Text.Json;
using System.Text.Json.Serialization;

namespace HaileyIntegration.Tech.Models.Dto;

internal static class AppJsonOptions
{
    internal static readonly JsonSerializerOptions Inbound = new() { PropertyNameCaseInsensitive = true };

    internal static readonly JsonSerializerOptions Outbound = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
