using HaileyIntegration.Tech.Models;

namespace HaileyIntegration.Tech.Services.Downstream;

public interface IHaileyService
{
    Task<IReadOnlyList<HaileyEmployee>> GetEmployeesAsync(CancellationToken ct = default);
    Task<HaileyCompany> GetCompanyAsync(CancellationToken ct = default);
}
