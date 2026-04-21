using HaileyIntegration.Tech.Models;

namespace HaileyIntegration.Tech.Services;

public interface IHaileyApiService
{
    Task<List<HaileyEmployee>> GetAllEmployeesAsync(CancellationToken ct = default);
}
