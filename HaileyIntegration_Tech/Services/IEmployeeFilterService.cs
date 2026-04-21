using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;

namespace HaileyIntegration.Tech.Services;

public interface IEmployeeFilterService
{
    FilterEmployeesResponse FilterByLastUpdated(FilterEmployeesRequest request);
}
