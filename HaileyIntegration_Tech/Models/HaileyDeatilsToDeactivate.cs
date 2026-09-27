using System.Text.Json.Serialization;

namespace HaileyIntegration.Tech.Models
{
    public class HaileyDeatilsToDeactivate
    {
        [JsonPropertyName("haileyEmployee")]
        public HaileyEmployee DeactivateHaileyEmployee { get; set; }
        [JsonPropertyName("haileyCompany")]
        public HaileyCompany HaileyCompany { get; set; }
    }

}
