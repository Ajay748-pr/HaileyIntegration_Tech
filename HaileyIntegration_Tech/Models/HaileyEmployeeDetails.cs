using System.Text.Json.Serialization;
namespace HaileyIntegration.Tech.Models;
public class HaileyEmployeeDetails
{
    //[JsonPropertyName("employeeId")]
    //public string EmployeeId { get; set; }

    //[JsonPropertyName("accountStatus")]
    //public string? AccountStatus { get; set; }

    //[JsonPropertyName("employmentStatus")]
    //public string? EmploymentStatus { get; set; }

    //[JsonPropertyName("lastUpdated")]
    //public DateTime LastUpdated { get; set; }

    [JsonPropertyName("jobData")]
    public JobData? JobData { get; set; }

    [JsonPropertyName("personal")]
    public Personal? Personal { get; set; }

    //[JsonPropertyName("compensation")]
    //public Compensation? Compensation { get; set; }

    //[JsonPropertyName("feedbackSessions")]
    //public List<object>? FeedbackSessions { get; set; }

    //[JsonPropertyName("feedback")]
    //public List<object>? Feedback { get; set; }

    //[JsonPropertyName("threeSixtyReviews")]
    //public List<object>? ThreeSixtyReviews { get; set; }

    [JsonPropertyName("salaries")]
    public List<Salary>? Salaries { get; set; }

    //[JsonPropertyName("organizationalBelonging")]
    //public OrganizationalBelonging? OrganizationalBelonging { get; set; }

    //[JsonPropertyName("customFieldsData")]
    //public Dictionary<string, object>? CustomFieldsData { get; set; }
}

public class JobData
{
    [JsonPropertyName("general")]
    public JobGeneral? General { get; set; }

    [JsonPropertyName("employment")]
    public Employment? Employment { get; set; }
}

public class JobGeneral
{
    //[JsonPropertyName("workPhone")]
    //public string? WorkPhone { get; set; }

    [JsonPropertyName("companyEmail")]
    public string? CompanyEmail { get; set; }

    [JsonPropertyName("employmentNumber")]
    public string? EmploymentNumber { get; set; }

    [JsonPropertyName("titles")]
    public List<string>? Titles { get; set; }

    //[JsonPropertyName("legalEntity")]
    //public string? LegalEntity { get; set; }
}

public class Employment
{
    [JsonPropertyName("dateOfJoining")]
    public DateOnly?DateOfJoining { get; set; }

    [JsonPropertyName("lastDayOfEmployment")]
    public DateOnly?LastDayOfEmployment { get; set; }

    [JsonPropertyName("employments")]
    public List<EmploymentItem>? Employments { get; set; }

    //[JsonPropertyName("casualEmployments")]
    //public List<object>? CasualEmployments { get; set; }
}

    public class EmploymentItem
    {
    [JsonPropertyName("employmentId")]
    public string EmploymentId { get; set; }

    //    [JsonPropertyName("priority")]
    //    public int? Priority { get; set; }

    [JsonPropertyName("startDate")]
    public DateOnly? StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateOnly? EndDate { get; set; }

    [JsonPropertyName("lastWorkingDay")]
    public DateOnly? LastWorkingDay { get; set; }

    [JsonPropertyName("organizationalInformation")]
    public OrganizationalInformation? OrganizationalInformation { get; set; }

    [JsonPropertyName("terms")]
    public Terms? Terms { get; set; }

    //    [JsonPropertyName("customFieldsData")]
    //    public Dictionary<string, string>? CustomFieldsData { get; set; }

    //    [JsonPropertyName("employmentSequenceNumber")]
    //    public string? EmploymentSequenceNumber { get; set; }

    //    [JsonPropertyName("status")]
    //    public string? Status { get; set; }
}

public class Terms
{
    //    [JsonPropertyName("employmentType")]
    //    public string? EmploymentType { get; set; }

    //    [JsonPropertyName("noticePeriodMonths")]
    //    public int? NoticePeriodMonths { get; set; }

    //    [JsonPropertyName("noticePeriod")]
    //    public int? NoticePeriod { get; set; }

    //    [JsonPropertyName("noticePeriodUnit")]
    //    public string? NoticePeriodUnit { get; set; }

    //    [JsonPropertyName("endOfProbationaryPeriod")]
    //    public DateOnly? EndOfProbationaryPeriod { get; set; }

    //    [JsonPropertyName("fixedTermType")]
    //    public string? FixedTermType { get; set; }

    //    [JsonPropertyName("endOfFixedTerm")]
    //    public DateOnly? EndOfFixedTerm { get; set; }

    [JsonPropertyName("scopePercentage")]
    public decimal? ScopePercentage { get; set; }

    //    [JsonPropertyName("scopeHours")]
    //    public decimal ScopeHours { get; set; }

    //    [JsonPropertyName("vacationDays")]
    //    public int? VacationDays { get; set; }

    //    [JsonPropertyName("substitutingForEmployeeId")]
    //    public string? SubstitutingForEmployeeId { get; set; }

    //    [JsonPropertyName("substituteReasonId")]
    //    public string? SubstituteReasonId { get; set; }
}

public class Personal
{
    [JsonPropertyName("general")]
    public PersonalGeneral? General { get; set; }

    [JsonPropertyName("sensitive")]
    public Sensitive? Sensitive { get; set; }

    [JsonPropertyName("contactInformation")]
    public ContactInformation? ContactInformation { get; set; }

    //[JsonPropertyName("ice")]
    //public Ice? Ice { get; set; }
}

public class PersonalGeneral
{
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }
}

public class Sensitive
{
    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("dateOfBirth")]
    public DateOnly? DateOfBirth { get; set; }

    [JsonPropertyName("personalIdentityNumber")]
    public string? PersonalIdentityNumber { get; set; }
}

public class ContactInformation
{
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
}

//public class Ice
//{
//    [JsonPropertyName("name")]
//    public string? Name { get; set; }

//    [JsonPropertyName("phone")]
//    public string? Phone { get; set; }

//    [JsonPropertyName("relation")]
//    public string? Relation { get; set; }
//}
public class Salary
{
    //[JsonPropertyName("salaryId")]
    //public string SalaryId { get; set; }

    //[JsonPropertyName("employeeId")]
    //public string EmployeeId { get; set; }

    //[JsonPropertyName("title")]
    //public string? Title { get; set; }

    //[JsonPropertyName("salaryTypeId")]
    //public string SalaryTypeId { get; set; }

    //[JsonPropertyName("salaryTypeName")]
    //public string? SalaryTypeName { get; set; }

    //[JsonPropertyName("payPeriod")]
    //public string? PayPeriod { get; set; }

    [JsonPropertyName("salaryType")]
    public string? SalaryType { get; set; }

    //[JsonPropertyName("currencyCode")]
    //public CurrencyCode? CurrencyCode { get; set; }

    //[JsonPropertyName("endDate")]
    //public DateOnly? EndDate { get; set; }

    [JsonPropertyName("history")]
    public List<SalaryHistory>? History { get; set; }
}

//public class CurrencyCode
//{
//    [JsonPropertyName("name")]
//    public string? Name { get; set; }
//}

//public class Compensation
//{
//    [JsonPropertyName("bankDetails")]
//    public BankDetails? BankDetails { get; set; }
//}

//public class BankDetails
//{
//    [JsonPropertyName("bankName")]
//    public string? BankName { get; set; }

//    [JsonPropertyName("clearingNumber")]
//    public string? ClearingNumber { get; set; }

//    [JsonPropertyName("accountNumber")]
//    public string? AccountNumber { get; set; }

//    [JsonPropertyName("iban")]
//    public string? Iban { get; set; }

//    [JsonPropertyName("bic")]
//    public string? Bic { get; set; }

//    [JsonPropertyName("swift")]
//    public string? Swift { get; set; }
//}

public class OrganizationalInformation
{
    [JsonPropertyName("titleIds")]
    public List<string>? TitleIds { get; set; }

    [JsonPropertyName("teamIds")]
    public List<string>? TeamIds { get; set; }

    [JsonPropertyName("locationId")]
    public string LocationId { get; set; }

    [JsonPropertyName("managerEmployeeId")]
    public string ManagerEmployeeId { get; set; }

    [JsonPropertyName("departmentId")]
    public string DepartmentId { get; set; }

    [JsonPropertyName("legalEntityId")]
    public string LegalEntityId { get; set; }

    [JsonPropertyName("businessAreaId")]
    public string? BusinessAreaId { get; set; }

    [JsonPropertyName("costCenterId")]
    public string CostCenterId { get; set; }
}

public class SalaryHistory
{
    [JsonPropertyName("date")]
    public DateOnly?Date { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}

