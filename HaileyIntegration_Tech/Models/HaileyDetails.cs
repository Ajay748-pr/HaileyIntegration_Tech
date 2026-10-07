using System.Text.Json.Serialization;
namespace HaileyIntegration.Tech.Models;
public class HaileyDeatils
{
    [JsonPropertyName("haileyEmployeeDetails")]
    public HaileyEmployeeDetails HaileyEmployeeDetails { get; set; }

    [JsonPropertyName("haileyCompany")]
    public HaileyCompany HaileyCompany { get; set; }
    [JsonPropertyName("haileyManagerEmployeeNumber")]
    public string HaileyManagerEmployeeNumber { get; set; }

    [JsonPropertyName("skipEmployee")]
    public string skipEmployee { get; set; }
}
