using System.Text.Json.Serialization;

namespace HaileyIntegration.Tech.Models;

public sealed class HaileyEmployee
{
    [JsonPropertyName("employeeId")]
    public Guid EmployeeId { get; set; }

    [JsonPropertyName("accountStatus")]
    public string? AccountStatus { get; set; }

    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; }

    [JsonPropertyName("workPhone")]
    public string? WorkPhone { get; set; }

    [JsonPropertyName("titleIds")]
    public List<Guid>? TitleIds { get; set; }

    [JsonPropertyName("companyEmail")]
    public string? CompanyEmail { get; set; }

    [JsonPropertyName("departmentId")]
    public Guid? DepartmentId { get; set; }

    [JsonPropertyName("locationId")]
    public Guid? LocationId { get; set; }

    [JsonPropertyName("legalEntityId")]
    public Guid? LegalEntityId { get; set; }

    [JsonPropertyName("businessAreaId")]
    public Guid? BusinessAreaId { get; set; }

    [JsonPropertyName("costCenterId")]
    public Guid? CostCenterId { get; set; }

    [JsonPropertyName("managerEmployeeId")]
    public Guid? ManagerEmployeeId { get; set; }

    [JsonPropertyName("teamIds")]
    public List<Guid>? TeamIds { get; set; }

    [JsonPropertyName("dateOfJoining")]
    public DateOnly? DateOfJoining { get; set; }

    [JsonPropertyName("lastDayOfEmployment")]
    public DateOnly? LastDayOfEmployment { get; set; }

    [JsonPropertyName("lastWorkingDay")]
    public DateOnly? LastWorkingDay { get; set; }

    [JsonPropertyName("employmentStatus")]
    public string? EmploymentStatus { get; set; }

    [JsonPropertyName("employmentType")]
    public string? EmploymentType { get; set; }

    [JsonPropertyName("noticePeriod")]
    public int? NoticePeriod { get; set; }

    [JsonPropertyName("noticePeriodUnit")]
    public string? NoticePeriodUnit { get; set; }

    [JsonPropertyName("endOfProbationaryPeriod")]
    public DateOnly? EndOfProbationaryPeriod { get; set; }

    [JsonPropertyName("fixedTermType")]
    public string? FixedTermType { get; set; }

    [JsonPropertyName("endOfFixedTerm")]
    public DateOnly? EndOfFixedTerm { get; set; }

    [JsonPropertyName("scopePercentage")]
    public decimal? ScopePercentage { get; set; }

    [JsonPropertyName("scopeHours")]
    public decimal? ScopeHours { get; set; }

    [JsonPropertyName("vacationDays")]
    public int? VacationDays { get; set; }

    [JsonPropertyName("substitutingForEmployeeId")]
    public Guid? SubstitutingForEmployeeId { get; set; }

    [JsonPropertyName("sustituteReasonId")]
    public Guid? SubstituteReasonId { get; set; }

    [JsonPropertyName("employmentNumber")]
    public string? EmploymentNumber { get; set; }

    [JsonPropertyName("bankName")]
    public string? BankName { get; set; }

    [JsonPropertyName("clearingNumber")]
    public string? ClearingNumber { get; set; }

    [JsonPropertyName("accountNumber")]
    public string? AccountNumber { get; set; }

    [JsonPropertyName("iban")]
    public string? Iban { get; set; }

    [JsonPropertyName("bic")]
    public string? Bic { get; set; }

    [JsonPropertyName("swift")]
    public string? Swift { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("dateOfBirth")]
    public string? DateOfBirth { get; set; }

    [JsonPropertyName("personalIdentityNumber")]
    public string? PersonalIdentityNumber { get; set; }

    [JsonPropertyName("privateEmail")]
    public string? PrivateEmail { get; set; }

    [JsonPropertyName("privatePhone")]
    public string? PrivatePhone { get; set; }

    [JsonPropertyName("streetAddress")]
    public string? StreetAddress { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("iceName")]
    public string? IceName { get; set; }

    [JsonPropertyName("icePhone")]
    public string? IcePhone { get; set; }

    [JsonPropertyName("iceRelation")]
    public string? IceRelation { get; set; }

    [JsonPropertyName("customFieldsData")]
    public Dictionary<string, List<string>>? CustomFieldsData { get; set; }

    [JsonPropertyName("employmentSequenceNumber")]
    public string? EmploymentSequenceNumber { get; set; }

    // Resolved by Logic App before calling this function
    [JsonPropertyName("reportingTo")]
    public string? ReportingTo { get; set; }

    // Resolved by Logic App: costCenterId GUID → getCompany.costCenters[].code
    [JsonPropertyName("extCostCentre")]
    public string? ExtCostCentre { get; set; }
}
