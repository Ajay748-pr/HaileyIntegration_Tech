using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using ServiceReference1;

namespace HaileyIntegration.Tech.Services.Downstream;

public interface IQuinyxService
{
    public string apiKey { get; set; }
    public string quinyxGroups { get; set; }
    Task<SyncResult> UpdateEmployeeAsync(UpdateEmployee employee, string apiKey, CancellationToken ct = default);

    Task<int?> GetAgreementIdAsync(string badgeNo, string apiKey, CancellationToken ct = default);

    Task<SyncResult> UpdateAgreementAsync(
    UpdateAgreementV2 agreement, string apiKey,
    CancellationToken ct = default);
    Task<SyncResult> MoveEmployeeAsync(
    moveEmployee employee, string apiKey,
    CancellationToken ct = default);

    Task<IReadOnlyList<AgreementTemplate>> GetAgreementTemplatesAsync(
        int agreementTemplateId = 0,
        string lastModified = "",
        CancellationToken ct = default);

    Task<IReadOnlyList<UnitKeyV2>> GetUnitsAPIKeyAsync( CancellationToken ct = default);
    
    //public string GetGroupList();

    Task<IReadOnlyList<Category>> GetCategoriesAsync(
        int categoryType = 0,
        string lastModified = "",
        CancellationToken ct = default);
}
