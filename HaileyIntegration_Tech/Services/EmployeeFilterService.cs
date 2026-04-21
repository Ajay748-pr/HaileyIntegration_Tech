using HaileyIntegration.Tech.Models.Dto;

namespace HaileyIntegration.Tech.Services;

public sealed class EmployeeFilterService : IEmployeeFilterService
{
    public FilterEmployeesResponse FilterByLastUpdated(FilterEmployeesRequest request)
    {
        var reference = (request.ReferenceTime ?? DateTime.UtcNow).ToUniversalTime();
        // Add a 5-minute buffer to catch records updated right at the boundary
        var windowStart = reference.AddHours(-request.WindowHours).AddMinutes(-5);

        var filtered = request.Employees
            .Where(e => e.LastUpdated.ToUniversalTime() >= windowStart)
            .ToList();

        return new FilterEmployeesResponse
        {
            Employees = filtered,
            TotalReceived = request.Employees.Count,
            FilteredCount = filtered.Count,
            WindowStart = windowStart,
            WindowEnd = reference
        };
    }
}
