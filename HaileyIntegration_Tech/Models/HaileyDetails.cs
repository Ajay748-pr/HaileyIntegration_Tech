using System.Text.Json.Serialization;
namespace HaileyIntegration.Tech.Models;
public class HaileyDeatils
{
    [JsonPropertyName("haileyEmployee")]
    public HaileyEmployee HaileyEmployee { get; set; }

    [JsonPropertyName("haileyEmployeeDetails")]
    public HaileyEmployeeDetails HaileyEmployeeDetails { get; set; }

    [JsonPropertyName("haileyCompany")]
    public HaileyCompany HaileyCompany { get; set; }
    [JsonPropertyName("haileyEmployeeManger")]
    public string HaileyMangerEmployeeNumber { get; set; }
}

