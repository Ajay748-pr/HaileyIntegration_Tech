using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;

namespace HaileyIntegration.Tech.Services.Downstream;

public interface IIdentityProvisioningService
{
    Task<SyncResult> ProvisionAsync(CanonicalEmployee employee, CancellationToken ct = default);
    Task<SyncResult> DeprovisionAsync(CanonicalEmployee employee, CancellationToken ct = default);
}
