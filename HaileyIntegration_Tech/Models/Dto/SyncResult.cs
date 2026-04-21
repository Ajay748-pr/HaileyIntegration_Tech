namespace HaileyIntegration.Tech.Models.Dto;

public sealed class SyncResult
{
    public bool Success { get; set; }
    public string? EmployeeNumber { get; set; }
    public string? TargetSystem { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
