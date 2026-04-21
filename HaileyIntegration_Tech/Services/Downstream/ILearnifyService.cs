using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;

namespace HaileyIntegration.Tech.Services.Downstream;

public interface ILearnifyService
{
    Task<SyncResult> SyncEmployeeAsync(CanonicalEmployee employee, CancellationToken ct = default);
}
