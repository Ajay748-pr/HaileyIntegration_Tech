namespace HaileyIntegration.Tech.Models;

public sealed class CanonicalEmployee
{
    // Master integration key — must be present in every downstream system
    public string EmployeeNumber { get; set; } = default!;
    public Guid HaileyEmployeeId { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }   // must be unique in AD
    public string? CompanyEmail { get; set; }
    public string? WorkPhone { get; set; }
    public string? Gender { get; set; }
    public string? DateOfBirth { get; set; }
    public string? PersonalIdentityNumber { get; set; }

    // Employment
    public string? EmploymentType { get; set; }
    public string? EmploymentStatus { get; set; }
    public string? AccountStatus { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly? LastWorkingDay { get; set; }

    // Organisational
    public Guid? DepartmentId { get; set; }
    public Guid? CostCenterId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? BusinessAreaId { get; set; }
    public Guid? ManagerEmployeeId { get; set; }
    public List<Guid>? TitleIds { get; set; }
    public List<Guid>? TeamIds { get; set; }

    // Payroll / contract (Primula)
    public decimal? ScopePercentage { get; set; }
    public decimal? ScopeHours { get; set; }
    public int? VacationDays { get; set; }
    public string? FixedTermType { get; set; }
    public DateOnly? EndOfFixedTerm { get; set; }
    public DateOnly? EndOfProbationaryPeriod { get; set; }

    // Finance (Visma / Primula)
    public string? CostCenter { get; set; }
    public string? Project { get; set; }
    public string? ActivityCode { get; set; }

    // Banking (Primula payroll)
    public string? BankName { get; set; }
    public string? ClearingNumber { get; set; }
    public string? AccountNumber { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }
    public string? Swift { get; set; }

    // Integration metadata
    public DateTime LastUpdated { get; set; }
    public ChangeType ChangeType { get; set; }
}

public enum ChangeType
{
    NewHire,
    Update,
    Termination,
    Reactivation
}
