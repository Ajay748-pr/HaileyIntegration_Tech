using System.Net;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace HaileyIntegration.Tech.Functions;

/// <summary>
/// Test/debug endpoint. Fetches Hailey data, runs all mapping logic, and returns
/// the mapped payloads as JSON — without calling Quinyx.
/// Use this to verify field mapping before triggering a live sync.
///
/// GET /api/sync/hailey-to-quinyx/preview
/// </summary>
public sealed class PreviewHaileyMappingFunction(
    IHaileyService haileyService,
    ILogger<PreviewHaileyMappingFunction> logger)
{
    [Function(nameof(PreviewHaileyMappingFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "sync/hailey-to-quinyx/preview")]
        HttpRequestData req,
        CancellationToken ct)
    {
        logger.LogInformation("PreviewHaileyMappingFunction triggered");

        // ── Fetch from Hailey ────────────────────────────────────────────────
        var employees = await haileyService.GetEmployeesAsync(ct);
        var company   = await haileyService.GetCompanyAsync(ct);

        // ── Build lookup dictionaries ────────────────────────────────────────
        var managerLookup = employees
            .Where(e => e.EmployeeId != Guid.Empty && !string.IsNullOrWhiteSpace(e.EmploymentNumber))
            .ToDictionary(e => e.EmployeeId, e => e.EmploymentNumber!);

        var costCentreLookup = (company.CostCenters ?? [])
            .Where(c => c.Id != Guid.Empty && !string.IsNullOrWhiteSpace(c.Code))
            .ToDictionary(c => c.Id, c => c.Code!);

        var departmentLookup = (company.Departments ?? [])
            .Where(d => d.Id != Guid.Empty && !string.IsNullOrWhiteSpace(d.Name))
            .ToDictionary(d => d.Id, d => d.Name!);

        // ── Map each employee — no Quinyx calls ──────────────────────────────
        var preview = employees
            .Where(e => !string.IsNullOrWhiteSpace(e.EmploymentNumber))
            .Select(emp =>
            {
                var reportingTo   = emp.ManagerEmployeeId.HasValue &&
                                    managerLookup.TryGetValue(emp.ManagerEmployeeId.Value, out var mgr)
                    ? mgr : null;

                var extCostCentre = emp.CostCenterId.HasValue &&
                                    costCentreLookup.TryGetValue(emp.CostCenterId.Value, out var cc)
                    ? cc : null;

                var extSectionId  = emp.DepartmentId.HasValue &&
                                    departmentLookup.TryGetValue(emp.DepartmentId.Value, out var dept)
                    ? dept : null;

                return new
                {
                    // ── Source (Hailey) ──────────────────────────────────────
                    source = new
                    {
                        employeeId        = emp.EmployeeId,
                        employmentNumber  = emp.EmploymentNumber,
                        firstName         = emp.FirstName,
                        lastName          = emp.LastName,
                        companyEmail      = emp.CompanyEmail,
                        workPhone         = emp.WorkPhone,
                        privatePhone      = emp.PrivatePhone,
                        streetAddress     = emp.StreetAddress,
                        postalCode        = emp.PostalCode,
                        city              = emp.City,
                        accountStatus     = emp.AccountStatus,
                        employmentType    = emp.EmploymentType,
                        dateOfJoining     = emp.DateOfJoining,
                        lastDayOfEmployment = emp.LastDayOfEmployment,
                        scopeHours        = emp.ScopeHours,
                        scopePercentage   = emp.ScopePercentage,
                        managerEmployeeId = emp.ManagerEmployeeId,
                        costCenterId      = emp.CostCenterId,
                        departmentId      = emp.DepartmentId,
                    },

                    // ── Resolved lookups ────────────────────────────────────
                    resolved = new
                    {
                        reportingTo,
                        extCostCentre,
                        extSectionId,
                    },

                    // ── Mapped: UpdateEmployee payload ──────────────────────
                    updateEmployee = new
                    {
                        badgeNo       = emp.EmploymentNumber,
                        givenName     = emp.FirstName,
                        familyName    = emp.LastName,
                        email         = emp.CompanyEmail,
                        phoneNo       = emp.WorkPhone,
                        cellPhone     = emp.PrivatePhone,
                        address1      = emp.StreetAddress,
                        zip           = emp.PostalCode,
                        city          = emp.City,
                        reportingTo,
                        extCostCentre,
                        extSectionId,
                        active        = string.Equals(emp.AccountStatus, "active", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
                        employedDate  = emp.DateOfJoining,
                        leaveDate     = emp.LastDayOfEmployment,
                    },

                    // ── Mapped: UpdateAgreement payload ─────────────────────
                    updateAgreement = new
                    {
                        badgeNo           = emp.EmploymentNumber,
                        extAgreementId    = emp.EmploymentType,
                        isMainAgreement   = true,
                        hourly            = false,
                        fullEmploymentHrs = emp.ScopeHours ?? 40m,
                        minHrsWeek        = emp.ScopeHours,
                        employmentRate    = emp.ScopePercentage.HasValue
                            ? emp.ScopePercentage.Value / 100m
                            : (decimal?)null,
                        fromDate          = emp.DateOfJoining,
                        expires           = emp.LastDayOfEmployment.HasValue,
                        toDate            = emp.LastDayOfEmployment,
                    },
                };
            })
            .ToList();

        logger.LogInformation("Preview generated for {Count} employee(s)", preview.Count);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            totalEmployees = employees.Count,
            mapped         = preview.Count,
            employees      = preview
        }, ct);

        return response;
    }
}
