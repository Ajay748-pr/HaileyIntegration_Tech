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

    // "Permanent" | "FixedTerm" | "ProbationaryPeriod"
    [JsonPropertyName("employmentType")]
    public string? EmploymentType { get; set; }

    // Actual contracted hours/week (scopeHours from getEmployee)
    [JsonPropertyName("scopeHours")]
    public decimal? ScopeHours { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("hourly")]
    public bool? Hourly { get; set; }

    [JsonPropertyName("isMainAgreement")]
    public bool? IsMainAgreement { get; set; }

    [JsonPropertyName("additionalField1")]
    public string? AdditionalField1 { get; set; }

    [JsonPropertyName("additionalField2")]
    public string? AdditionalField2 { get; set; }

    [JsonPropertyName("additionalField3")]
    public string? AdditionalField3 { get; set; }

    [JsonPropertyName("additionalField4")]
    public string? AdditionalField4 { get; set; }

    [JsonPropertyName("additionalField5")]
    public string? AdditionalField5 { get; set; }
}