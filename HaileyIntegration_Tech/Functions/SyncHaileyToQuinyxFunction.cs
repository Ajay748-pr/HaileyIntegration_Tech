using System.Net;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ServiceReference1;

namespace HaileyIntegration.Tech.Functions;

/// <summary>
/// Pulls all employees + company data from Hailey, resolves manager/cost-centre lookups,
/// then pushes each employee into Quinyx via UpdateEmployee + UpdateAgreement SOAP calls.
///
/// Trigger: POST /api/sync/hailey-to-quinyx
/// To run on a schedule instead, replace [HttpTrigger] with:
///   [TimerTrigger("0 0 6 * * *")]  // every day at 06:00 UTC
/// </summary>
public sealed class SyncHaileyToQuinyxFunction(
    IHaileyService haileyService,
    IQuinyxService quinyxService,
    ILogger<SyncHaileyToQuinyxFunction> logger)
{
    [Function(nameof(SyncHaileyToQuinyxFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync/hailey-to-quinyx")]
        HttpRequestData req,
        CancellationToken ct)
    {
        logger.LogInformation(
            "SyncHaileyToQuinyxFunction triggered. RequestId={RequestId}",
            req.FunctionContext.InvocationId);

        // ── Step 1: Fetch from Hailey ─────────────────────────────────────────
        var employees = await haileyService.GetEmployeesAsync(ct);
        var company   = await haileyService.GetCompanyAsync(ct);

        logger.LogInformation(
            "Hailey data loaded. Employees={EmployeeCount} CostCenters={CostCenterCount}",
            employees.Count, company.CostCenters?.Count ?? 0);

        // ── Step 2: Build lookup dictionaries ────────────────────────────────
        // employeeId  → employmentNumber  (for resolving manager's badge number)
        var managerLookup = employees
            .Where(e => e.EmployeeId != Guid.Empty && !string.IsNullOrWhiteSpace(e.EmploymentNumber))
            .ToDictionary(e => e.EmployeeId, e => e.EmploymentNumber!);

        // costCenterId → costCenter.code  (extCostCentre for Quinyx)
        var costCentreLookup = (company.CostCenters ?? [])
            .Where(c => c.Id != Guid.Empty && !string.IsNullOrWhiteSpace(c.Code))
            .ToDictionary(c => c.Id, c => c.Code!);

        // departmentId → department.name  (extSectionId for Quinyx)
        var departmentLookup = (company.Departments ?? [])
            .Where(d => d.Id != Guid.Empty && !string.IsNullOrWhiteSpace(d.Name))
            .ToDictionary(d => d.Id, d => d.Name!);

        // ── Step 3: Map employees (safe per-employee try-catch) ──────────────
        var succeeded = 0;
        var failed    = 0;
        var skipped   = 0;

        var mapped = new List<MappedEmployee>();

        foreach (var employee in employees)
        {
            try
            {
                var result = MapEmployee(employee, managerLookup, costCentreLookup, departmentLookup);

                if (result == null)
                {
                    logger.LogWarning(
                        "Employee skipped. EmployeeId={EmployeeId}, EmploymentNumber={EmploymentNumber}",
                        employee.EmployeeId,
                        employee.EmploymentNumber);

                    skipped++;
                    continue;
                }

                mapped.Add(result);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to map employee. EmployeeId={EmployeeId}, EmploymentNumber={EmploymentNumber}",
                    employee.EmployeeId,
                    employee.EmploymentNumber);

                failed++;
            }
        }

        // ── Step 4: Push each mapped employee to Quinyx ───────────────────────
        foreach (var emp in mapped)
        {
            logger.LogInformation(
                "Processing employee {EmploymentNumber} ReportingTo={ReportingTo} ExtCostCentre={ExtCostCentre}",
                emp.Employee.badgeNo, emp.Employee.reportingTo ?? "(none)", emp.Employee.extCostCentre ?? "(none)");

            var empResult = await quinyxService.UpdateEmployeeAsync(emp.Employee, ct);

            if (!empResult.Success)
            {
                logger.LogWarning(
                    "UpdateEmployee failed for {EmploymentNumber}: {Message}",
                    emp.Employee.badgeNo, empResult.Message);
                failed++;
                continue;
            }

            var agreeResult = await quinyxService.UpdateAgreementAsync(emp.Agreement, ct);

            if (agreeResult.Success)
                succeeded++;
            else
            {
                logger.LogWarning(
                    "UpdateAgreement failed for {EmploymentNumber}: {Message}",
                    emp.Agreement.badgeNo, agreeResult.Message);
                failed++;
            }
        }

        logger.LogInformation(
            "Sync complete. Succeeded={Succeeded} Failed={Failed} Skipped={Skipped}",
            succeeded, failed, skipped);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            total     = employees.Count,
            succeeded,
            failed,
            skipped
        }, ct);

        return response;
    }

    // ── Holds both SOAP objects for one employee ──────────────────────────────
    private sealed record MappedEmployee(UpdateEmployee Employee, UpdateAgreementV2 Agreement);

    // ── Resolve lookups + map → returns null if employee must be skipped ──────
    private static MappedEmployee? MapEmployee(
        HaileyEmployee src,
        Dictionary<Guid, string> managerLookup,
        Dictionary<Guid, string> costCentreLookup,
        Dictionary<Guid, string> departmentLookup)
    {
        if (string.IsNullOrWhiteSpace(src.EmploymentNumber))
            return null;

        src.ReportingTo = src.ManagerEmployeeId.HasValue &&
                          managerLookup.TryGetValue(src.ManagerEmployeeId.Value, out var mgr)
            ? mgr : null;

        src.ExtCostCentre = src.CostCenterId.HasValue &&
                            costCentreLookup.TryGetValue(src.CostCenterId.Value, out var cc)
            ? cc : null;

        var extSectionId = src.DepartmentId.HasValue &&
                           departmentLookup.TryGetValue(src.DepartmentId.Value, out var dept)
            ? dept : null;

        return new MappedEmployee(MapToUpdateEmployee(src, extSectionId), MapToUpdateAgreement(src));
    }

    // ── Mapping: HaileyEmployee → Quinyx UpdateEmployee ───────────────────────
    private static UpdateEmployee MapToUpdateEmployee(HaileyEmployee src, string? extSectionId)
    {
        var dest = new UpdateEmployee
        {
            badgeNo       = src.EmploymentNumber,
            givenName     = src.FirstName,
            familyName    = src.LastName,
            email         = src.CompanyEmail,
            phoneNo       = src.WorkPhone,
            cellPhone     = src.PrivatePhone,
            address1      = src.StreetAddress,
            zip           = src.PostalCode,
            city          = src.City,
            reportingTo   = src.ReportingTo,
            extCostCentre = src.ExtCostCentre,
            extSectionId  = extSectionId,
        };

        if (!string.IsNullOrWhiteSpace(src.DateOfBirth) &&
            DateTime.TryParse(src.DateOfBirth, out var dob))
        {
            dest.dateOfBirth          = dob;
            dest.dateOfBirthSpecified = true;
        }

        if (src.DateOfJoining.HasValue)
        {
            dest.employedDate          = src.DateOfJoining.Value.ToDateTime(TimeOnly.MinValue);
            dest.employedDateSpecified = true;
        }

        if (src.LastDayOfEmployment.HasValue)
        {
            dest.leaveDate          = src.LastDayOfEmployment.Value.ToDateTime(TimeOnly.MinValue);
            dest.leaveDateSpecified = true;
        }

        if (!string.IsNullOrWhiteSpace(src.AccountStatus))
        {
            dest.active          = string.Equals(src.AccountStatus, "active", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            dest.activeSpecified = true;
        }

        return dest;
    }

    // ── Mapping: HaileyEmployee → Quinyx UpdateAgreementV2 ───────────────────
    private static UpdateAgreementV2 MapToUpdateAgreement(HaileyEmployee src)
    {
        var dest = new UpdateAgreementV2
        {
            badgeNo                  = src.EmploymentNumber,
            extAgreementId           = src.EmploymentType,   // "Permanent" | "FixedTerm" | "ProbationaryPeriod"
            isMainAgreement          = true,
            isMainAgreementSpecified = true,
            hourly                   = false,
            hourlySpecified          = true,
            fullEmploymentHrs          = src.ScopeHours ?? 40m,
            fullEmploymentHrsSpecified = true,
        };

        // fromDate → dateOfJoining
        if (src.DateOfJoining.HasValue)
        {
            dest.fromDate          = src.DateOfJoining.Value.ToDateTime(TimeOnly.MinValue);
            dest.fromDateSpecified = true;
        }

        // toDate → lastDayOfEmployment (set Specified only when value is present)
        if (src.LastDayOfEmployment.HasValue)
        {
            dest.toDate          = src.LastDayOfEmployment.Value.ToDateTime(TimeOnly.MinValue);
            dest.toDateSpecified = true;
        }

        // expires: true if employment has an end date
        dest.expires          = src.LastDayOfEmployment.HasValue;
        dest.expiresSpecified = true;

        // minHrsWeek → scopeHours (contracted hours/week)
        if (src.ScopeHours.HasValue)
        {
            dest.minHrsWeek          = src.ScopeHours.Value;
            dest.minHrsWeekSpecified = true;
        }

        // employmentRatesAdd → scopePercentage (Quinyx rate is 0–1, Hailey is 0–100)
        if (src.ScopePercentage.HasValue && src.DateOfJoining.HasValue)
        {
            dest.employmentRatesAdd =
            [
                new EmploymentRate
                {
                    fromDate = src.DateOfJoining.Value.ToDateTime(TimeOnly.MinValue),
                    rate     = src.ScopePercentage.Value / 100m
                }
            ];
        }

        return dest;
    }
}
