using System.Text.Json.Serialization;

namespace HaileyIntegration.Tech.Models.Dto;

public sealed class HaileyAgreement
{
    [JsonPropertyName("employmentNumber")]
    public string? EmploymentNumber { get; set; }

    [JsonPropertyName("externalAgreementId")]
    public string? ExternalAgreementId { get; set; }

    [JsonPropertyName("externalTemplateId")]
    public string? ExternalTemplateId { get; set; }

    [JsonPropertyName("fromDate")]
    public DateOnly? FromDate { get; set; }

    [JsonPropertyName("toDate")]
    public DateOnly? ToDate { get; set; }

    [JsonPropertyName("expires")]
    public bool? Expires { get; set; }

    [JsonPropertyName("employmentRate")]
    public decimal? EmploymentRate { get; set; }

    [JsonPropertyName("hourlySalary")]
    public decimal? HourlySalary { get; set; }
}