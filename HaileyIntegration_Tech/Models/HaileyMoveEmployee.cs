using System.Text.Json.Serialization;

namespace HaileyIntegration.Tech.Models.Dto;

public sealed class HaileyMoveEmployee
{
    [JsonPropertyName("employmentNumber")]
    public string? EmploymentNumber { get; set; }

    [JsonPropertyName("sharableOnNewUnitFrom")]
    public string? SharableOnNewUnitFrom { get; set; }

    [JsonPropertyName("newUnitStartDate")]
    public string? NewUnitStartDate { get; set; }

    [JsonPropertyName("oldUnitEndShareDate")]
    public string? OldUnitEndShareDate { get; set; }

    [JsonPropertyName("unitExtCode")]
    public string? UnitExtCode { get; set; }

    [JsonPropertyName("reportingTo")]
    public string? ReportingTo { get; set; }

    [JsonPropertyName("section")]
    public string? Section { get; set; }

    [JsonPropertyName("costCentre")]
    public string? CostCentre { get; set; }
}