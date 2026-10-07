namespace HaileyIntegration.Tech.Models.Dto;

public sealed record VismaOptions(
    string TokenUrl,
    string ClientId,
    string ClientSecret,
    string Scope,
    string TenantId);
