using HaileyIntegration.Tech.Models;

namespace HaileyIntegration.Tech.Services;

public interface IEmployeeMappingService
{
    CanonicalEmployee MapToCanonical(HaileyEmployee source, ChangeType changeType);
}
