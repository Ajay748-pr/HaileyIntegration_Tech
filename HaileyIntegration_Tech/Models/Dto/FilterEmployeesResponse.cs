using HaileyIntegration.Tech.Models;

namespace HaileyIntegration.Tech.Models.Dto;

public sealed class FilterEmployeesResponse
{
    public List<HaileyEmployee> Employees { get; set; } = [];
    public int TotalReceived { get; set; }
    public int FilteredCount { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
}
