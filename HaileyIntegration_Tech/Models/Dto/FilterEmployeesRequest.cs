using HaileyIntegration.Tech.Models;

namespace HaileyIntegration.Tech.Models.Dto;

public sealed class FilterEmployeesRequest
{
    public List<HaileyEmployee> Employees { get; set; } = [];
    public int WindowHours { get; set; } = 6;
    // Optional override — defaults to UtcNow when absent
    public DateTime? ReferenceTime { get; set; }
}
