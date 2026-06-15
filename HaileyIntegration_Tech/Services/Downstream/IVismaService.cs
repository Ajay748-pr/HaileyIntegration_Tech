using HaileyIntegration.Tech.Models.Dto;

namespace HaileyIntegration.Tech.Services.Downstream;

public interface IVismaService
{
    Task<VismaResult> SyncDimensionAsync(VismaDimensionRequest request, CancellationToken ct = default);
}
