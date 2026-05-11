using HaileyIntegration.Tech.Models;

namespace HaileyIntegration.Tech.Services;

public sealed class EmployeeMappingService : IEmployeeMappingService
{
    public CanonicalEmployee MapToCanonical(HaileyEmployee source, ChangeType changeType)
    {
        if (string.IsNullOrWhiteSpace(source.EmploymentNumber))
            throw new InvalidOperationException(
                $"EmploymentNumber is required for employee {source.EmployeeId}. This is the master integration key.");

        return new CanonicalEmployee
        {
            EmployeeNumber = source.EmploymentNumber,
            HaileyEmployeeId = source.EmployeeId,
            FirstName = source.FirstName,
            LastName = source.LastName,
            DisplayName = BuildUniqueDisplayName(source),
            CompanyEmail = source.CompanyEmail,
            WorkPhone = source.WorkPhone,
            Gender = source.Gender,
            DateOfBirth = source.DateOfBirth,
            PersonalIdentityNumber = source.PersonalIdentityNumber,
            CompanyName = source.CompanyName,
            EmploymentType = source.EmploymentType,
            EmploymentStatus = source.EmploymentStatus,
            AccountStatus = source.AccountStatus,
            StartDate = source.DateOfJoining,
            EndDate = source.LastDayOfEmployment,
            LastWorkingDay = source.LastWorkingDay,
            DepartmentId = source.DepartmentId,
            CostCenterId = source.CostCenterId,
            LocationId = source.LocationId,
            BusinessAreaId = source.BusinessAreaId,
            ManagerEmployeeId = source.ManagerEmployeeId,
            TitleIds = source.TitleIds,
            TeamIds = source.TeamIds,
            ScopePercentage = source.ScopePercentage,
            ScopeHours = source.ScopeHours,
            VacationDays = source.VacationDays,
            FixedTermType = source.FixedTermType,
            EndOfFixedTerm = source.EndOfFixedTerm,
            EndOfProbationaryPeriod = source.EndOfProbationaryPeriod,
            BankName = source.BankName,
            ClearingNumber = source.ClearingNumber,
            AccountNumber = source.AccountNumber,
            Iban = source.Iban,
            Bic = source.Bic,
            Swift = source.Swift,
            LastUpdated = source.LastUpdated,
            ChangeType = changeType
        };
    }

    // AD constraint: duplicate FirstName + LastName combinations are rejected.
    // Append employment number to guarantee uniqueness.
    private static string BuildUniqueDisplayName(HaileyEmployee e)
    {
        var base_ = $"{e.FirstName} {e.LastName}".Trim();
        return string.IsNullOrWhiteSpace(e.EmploymentNumber)
            ? base_
            : $"{base_} ({e.EmploymentNumber})";
    }
}
