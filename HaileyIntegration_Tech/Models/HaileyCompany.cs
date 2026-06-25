using System.Text.Json.Serialization;

namespace HaileyIntegration.Tech.Models;

public sealed class HaileyCompany
{
    [JsonPropertyName("departments")]
    public List<HaileyDepartment> Departments { get; set; }

    //[JsonPropertyName("costCenters")]
    //public List<HaileyCostCenter> CostCenters { get; set; }

    //[JsonPropertyName("locations")]
    //public List<HaileyNamedItem> Locations { get; set; }

    //[JsonPropertyName("legalEntities")]
    //public List<HaileyNamedItem> LegalEntities { get; set; }

    //[JsonPropertyName("titles")]
    //public List<HaileyNamedItem> Titles { get; set; }

    //[JsonPropertyName("businessAreas")]
    //public List<HaileyNamedItem> BusinessAreas { get; set; }

    //[JsonPropertyName("teams")]
    //public List<HaileyNamedItem> Teams { get; set; }

    //[JsonPropertyName("customFields")]
    //public List<HaileyCustomField> CustomFields { get; set; }

    //[JsonPropertyName("customEmploymentFields")]
    //public List<HaileyCustomField> CustomEmploymentFields { get; set; }
}

public sealed class HaileyDepartment
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    //[JsonPropertyName("headOfDepartmentEmployeeId")]
    //public string? HeadOfDepartmentEmployeeId { get; set; }

    [JsonPropertyName("belongingToDepartmentId")]
    public string? BelongingToDepartmentId { get; set; }
}

//public sealed class HaileyNamedItem
//{
//    [JsonPropertyName("id")]
//    public string Id { get; set; }

//    [JsonPropertyName("name")]
//    public string? Name { get; set; }
//}

//public sealed class HaileyCostCenter
//{
//    [JsonPropertyName("id")]
//    public string Id { get; set; }

//    [JsonPropertyName("name")]
//    public string? Name { get; set; }

//    [JsonPropertyName("code")]
//    public string? Code { get; set; }
//}

//public sealed class HaileyCustomField
//{
//    [JsonPropertyName("id")]
//    public string Id { get; set; }

//    [JsonPropertyName("name")]
//    public string? Name { get; set; }

//    [JsonPropertyName("type")]
//    public string? Type { get; set; }
//}
