using System.Net.Http.Json;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using Microsoft.Extensions.Logging;
using ServiceReference1;

namespace HaileyIntegration.Tech.Services.Downstream;

public sealed class QuinyxService(HttpClient http, QuinyxOptions options, ILogger<QuinyxService> logger) : IQuinyxService
{
    public string apiKey { get; set; } = options.ApiKey;
    public string quinyxGroups { get; set; } = options.QuinyxGroups;

    // ─── GetAgreementId ──────────────────────────────────────────────────────
    // Fetches the first active agreement ID for a given badge number.
    // Used before UpdateAgreementV2 so Quinyx can locate the correct agreement record.
    public async Task<int?> GetAgreementIdAsync(string badgeNo,string apiKey, CancellationToken ct = default)
    {
        var client = new FlexForcePortTypeClient();
        try
        {
            var response = await client.wsdlGetAgreementsAsync( apiKey, 0, 0, badgeNo, "");
            await client.CloseAsync();

            var agreement = response?.@return?.FirstOrDefault();
            if (agreement == null)
            {
                logger.LogWarning("No agreements found in Quinyx for badgeNo={BadgeNo}", badgeNo);
                return null;
            }

            logger.LogInformation(
                "Found agreement id={Id} for badgeNo={BadgeNo}", agreement.id, badgeNo);

            return agreement.id;
        }
        catch (Exception ex)
        {
            client.Abort();
            logger.LogError(ex, "GetAgreementId threw for badgeNo={BadgeNo}", badgeNo);
            throw;
        }
    }

    // ─── UpdateEmployee ───────────────────────────────────────────────────────

    public async Task<SyncResult> UpdateEmployeeAsync(UpdateEmployee employee, string apiKey, CancellationToken ct = default)
    {
        logger.LogInformation(
            "UpdateEmployeeAsync starting for badgeNo={BadgeNo}", employee.badgeNo);

        var result = await TriggerUpdateEmployeeSoapAsync(employee,apiKey, ct);

        logger.LogInformation(
            "UpdateEmployeeAsync finished for badgeNo={BadgeNo} Success={Success}",
            employee.badgeNo, result.Success);

        return result;
    }

    // Trigger: creates the SOAP client, calls wsdlUpdateEmployees, and returns a SyncResult.
    // This is kept separate so it can be tested or retried independently.
    private async Task<SyncResult> TriggerUpdateEmployeeSoapAsync(UpdateEmployee employee, string apiKey, CancellationToken ct)
    {
        var client = new FlexForcePortTypeClient();
        try
        {
            logger.LogInformation(
                "Triggering SOAP wsdlUpdateEmployees for badgeNo={BadgeNo} givenName={GivenName} familyName={FamilyName} email={Email} phoneNo={PhoneNo} employedDate={EmployedDate} leaveDate={LeaveDate}",
                employee.badgeNo, employee.givenName, employee.familyName, employee.email,
                employee.phoneNo,
                employee.employedDateSpecified ? employee.employedDate.ToString("yyyy-MM-dd") : "(not set)",
                employee.leaveDateSpecified    ? employee.leaveDate.ToString("yyyy-MM-dd")    : "(not set)");

            var response = await client.wsdlUpdateEmployeesAsync(apiKey, [employee]);

            await client.CloseAsync();

            var returned = response.@return;

            if (returned is null || returned.Length == 0)
            {
                logger.LogWarning(
                    "Quinyx wsdlUpdateEmployees returned empty response for badgeNo={BadgeNo}", employee.badgeNo);

                return new SyncResult
                {
                    Success        = false,
                    EmployeeNumber = employee.badgeNo,
                    TargetSystem   = "Quinyx",
                    ErrorCode      = "EMPTY_RESPONSE",
                    Message        = "Quinyx returned no employee records in the response."
                };
            }

            logger.LogInformation(
                "Quinyx wsdlUpdateEmployees succeeded for badgeNo={BadgeNo}. Records returned={Count}",
                employee.badgeNo, returned.Length);

            return new SyncResult
            {
                Success        = true,
                EmployeeNumber = employee.badgeNo,
                TargetSystem   = "Quinyx",
                Message        = $"Updated successfully. Records in response: {returned.Length}"
            };
        }
        catch (Exception ex)
        {
            client.Abort();
            logger.LogError(
                ex,
                "SOAP wsdlUpdateEmployees threw for badgeNo={BadgeNo}: {Message}",
                employee.badgeNo, ex.Message);

            return new SyncResult
            {
                Success        = false,
                EmployeeNumber = employee.badgeNo,
                TargetSystem   = "Quinyx",
                ErrorCode      = "EXCEPTION",
                Message        = ex.Message
            };
        }
    }


    public async Task<SyncResult> UpdateAgreementAsync(
    UpdateAgreementV2 agreement,
    string apiKey, CancellationToken ct = default)
    {
        logger.LogInformation(
            "UpdateAgreementAsync starting for badgeNo={BadgeNo}",
            agreement.badgeNo);

        var client = new FlexForcePortTypeClient();

        try
        {
            logger.LogInformation(
                "Triggering SOAP wsdlUpdateAgreementsV2 for badgeNo={BadgeNo}",
                agreement.badgeNo);

            var response =
                await client.wsdlUpdateAgreementsV2Async(
                    apiKey,
                    [agreement]);

            await client.CloseAsync();

            var returned = response.@return;

            if (returned == null || returned.Length == 0)
            {
                logger.LogWarning(
                    "Quinyx returned empty response for badgeNo={BadgeNo}",
                    agreement.badgeNo);

                return new SyncResult
                {
                    Success = false,
                    EmployeeNumber = agreement.badgeNo,
                    TargetSystem = "Quinyx",
                    ErrorCode = "EMPTY_RESPONSE",
                    Message = "No agreement records returned from Quinyx."
                };
            }

            var updatedAgreement = returned.First();

            if (updatedAgreement.validationErrors?.Length > 0)
            {
                var validationMessage =
                    string.Join(", ", updatedAgreement.validationErrors);

                logger.LogWarning(
                    "Agreement validation failed for badgeNo={BadgeNo}. Errors={Errors}",
                    agreement.badgeNo,
                    validationMessage);

                return new SyncResult
                {
                    Success = false,
                    EmployeeNumber = agreement.badgeNo,
                    TargetSystem = "Quinyx",
                    ErrorCode = "VALIDATION_ERROR",
                    Message = validationMessage
                };
            }

            logger.LogInformation(
                "Agreement updated successfully for badgeNo={BadgeNo}",
                agreement.badgeNo);

            return new SyncResult
            {
                Success = true,
                EmployeeNumber = agreement.badgeNo,
                TargetSystem = "Quinyx",
                Message = "Agreement updated successfully."
            };
        }
        catch (Exception ex)
        {
            client.Abort();

            logger.LogError(
                ex,
                "SOAP wsdlUpdateAgreementsV2 failed for badgeNo={BadgeNo}",
                agreement?.badgeNo);

            logger.LogError(
                "Full Exception: {Exception}",
                ex.ToString());

            return new SyncResult
            {
                Success = false,
                EmployeeNumber = agreement?.badgeNo,
                TargetSystem = "Quinyx",
                ErrorCode = "EXCEPTION",
                Message = ex.ToString() // temporary for debugging
            };
        }



    }

    public async Task<SyncResult> MoveEmployeeAsync(
    moveEmployee employee,
    string apiKey, CancellationToken ct = default)
    {
        var client = new FlexForcePortTypeClient();

        try
        {
            logger.LogInformation(
                "Triggering wsdlMoveEmployees for badgeNo={BadgeNo}",
                employee.badgeNo);

            var response =
                await client.wsdlMoveEmployeesAsync(
                    apiKey,
                    [employee]);

            await client.CloseAsync();

            var returned = response.@return;

            if (returned == null || returned.Length == 0)
            {
                return new SyncResult
                {
                    Success = false,
                    EmployeeNumber = employee.badgeNo,
                    TargetSystem = "Quinyx",
                    ErrorCode = "EMPTY_RESPONSE",
                    Message = "No move response returned from Quinyx."
                };
            }

            var movedEmployee = returned.First();

            if (movedEmployee.validationErrors?.Length > 0)
            {
                return new SyncResult
                {
                    Success = false,
                    EmployeeNumber = employee.badgeNo,
                    TargetSystem = "Quinyx",
                    ErrorCode = "VALIDATION_ERROR",
                    Message = string.Join(
                        ", ",
                        movedEmployee.validationErrors)
                };
            }

            return new SyncResult
            {
                Success = true,
                EmployeeNumber = employee.badgeNo,
                TargetSystem = "Quinyx",
                Message =
                    $"Move scheduled successfully. MoveId={movedEmployee.moveId}"
            };
        }
        catch (Exception ex)
        {
            client.Abort();

            logger.LogError(
                ex,
                "MoveEmployee failed for badgeNo={BadgeNo}",
                employee.badgeNo);

            return new SyncResult
            {
                Success = false,
                EmployeeNumber = employee.badgeNo,
                TargetSystem = "Quinyx",
                ErrorCode = "EXCEPTION",
                Message = ex.Message
            };
        }
    }

    public async Task<IReadOnlyList<UnitKeyV2>> GetUnitsAPIKeyAsync( CancellationToken ct = default)
    {
        var client = new FlexForcePortTypeClient();
        try
        {
            logger.LogInformation("GetUnitsAPIKey starting.");
            var response = await client.wsdlGetUnitsAPIKeyV2Async(options.ApiKey);
            await client.CloseAsync();

            var units = response?.@return;
            if (units == null || units.Length == 0)
            {
                logger.LogInformation("Quinyx returned no units.");
                return [];
            }

            logger.LogInformation("Quinyx returned {Count} unit(s).", units.Length);
            return units;
        }
        catch (Exception ex)
        {
            client.Abort();
            logger.LogError(ex, "GetUnitsAPIKey threw.");
            throw;
        }
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(
        int categoryType = 0,
        string lastModified = "",
        CancellationToken ct = default)
    {
        var client = new FlexForcePortTypeClient();
        try
        {
            logger.LogInformation(
                "GetCategories starting. categoryType={CategoryType}", categoryType);

            var response = await client.wsdlGetCategoriesAsync(options.ApiKey, categoryType, lastModified);
            await client.CloseAsync();

            var categories = response?.@return;
            if (categories == null || categories.Length == 0)
            {
                logger.LogInformation("Quinyx returned no categories.");
                return [];
            }

            logger.LogInformation("Quinyx returned {Count} category(s).", categories.Length);
            return categories;
        }
        catch (Exception ex)
        {
            client.Abort();
            logger.LogError(ex, "GetCategories threw for categoryType={CategoryType}", categoryType);
            throw;
        }
    }

    public async Task<IReadOnlyList<AgreementTemplate>> GetAgreementTemplatesAsync(
        int agreementTemplateId = 0,
        string lastModified = "",
        CancellationToken ct = default)
    {
        var client = new FlexForcePortTypeClient();
        try
        {
            logger.LogInformation(
                "GetAgreementTemplates starting. agreementTemplateId={TemplateId} lastModified={LastModified}",
                agreementTemplateId, lastModified);

            var response = await client.wsdlGetAgreementTemplatesAsync(
                options.ApiKey, agreementTemplateId, lastModified);

            await client.CloseAsync();

            var templates = response?.@return;

            if (templates == null || templates.Length == 0)
            {
                logger.LogInformation("Quinyx returned no agreement templates.");
                return [];
            }

            logger.LogInformation("Quinyx returned {Count} agreement template(s).", templates.Length);
            return templates;
        }
        catch (Exception ex)
        {
            client.Abort();
            logger.LogError(ex, "GetAgreementTemplates threw for agreementTemplateId={TemplateId}", agreementTemplateId);
            throw;
        }
    }
}
