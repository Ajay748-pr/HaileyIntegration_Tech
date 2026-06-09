using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using ServiceReference1;

namespace HaileyIntegration.Tech.Services.Downstream;

public interface IQuinyxService
{
    Task<SyncResult> SyncEmployeeAsync(CanonicalEmployee employee, CancellationToken ct = default);
    Task<IReadOnlyList<QuinyxRestaurant>> GetRestaurantsAsync(string changedSince, CancellationToken ct = default);
    Task<SyncResult> UpdateEmployeeAsync(UpdateEmployee employee, CancellationToken ct = default);

    Task<SyncResult> UpdateAgreementAsync(
    UpdateAgreementV2 agreement,
    CancellationToken ct = default);
    Task<SyncResult> MoveEmployeeAsync(
    moveEmployee employee,
    CancellationToken ct = default);
}
