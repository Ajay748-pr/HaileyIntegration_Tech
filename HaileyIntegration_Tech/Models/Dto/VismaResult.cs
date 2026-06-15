namespace HaileyIntegration.Tech.Models.Dto;

public sealed class VismaResult
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
}
