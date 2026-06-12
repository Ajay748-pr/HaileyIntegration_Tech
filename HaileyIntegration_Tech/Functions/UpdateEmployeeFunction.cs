using System.Net;
using System.Text.Json;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using HaileyIntegration.Tech.Services.Downstream;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ServiceReference1;

namespace HaileyIntegration.Tech.Functions;

public sealed class UpdateEmployeeFunction(
    IQuinyxService quinyxService,
    ILogger<UpdateEmployeeFunction> logger)
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    // ─── Azure Function entry point ───────────────────────────────────────────

    [Function(nameof(UpdateEmployeeFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "quinyx/updateemployee")]
        HttpRequestData req,
        CancellationToken ct)
    {
        logger.LogInformation("UpdateEmployeeFunction triggered. RequestId={RequestId}", req.FunctionContext.InvocationId);

        // Step 1 – read and deserialise the incoming JSON body
        var employee = await ReadRequestAsync(req, ct);
        if (employee is null)
        {
            logger.LogWarning("Request body was empty or could not be deserialised.");
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("A valid HaileyEmployee JSON payload is required.", ct);
            return bad;
        }

        if (string.IsNullOrWhiteSpace(employee.EmploymentNumber))
        {
            logger.LogWarning("Missing required field: employmentNumber.");
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("employmentNumber is required.", ct);
            return bad;
        }

        logger.LogInformation(
            "Received update request for employee {EmploymentNumber} (HaileyId={HaileyId})",
            employee.EmploymentNumber, employee.EmployeeId);

        // Step 2 – map Hailey fields → Quinyx UpdateEmployee SOAP type
        var quinyxEmployee = MapToQuinyxEmployee(employee);

        // Step 3 – call the Quinyx SOAP service
        var result = await CallUpdateEmployeeAsync(quinyxEmployee, ct);

        logger.LogInformation(
            "UpdateEmployee completed. Success={Success} EmployeeNumber={EmployeeNumber} Message={Message}",
            result.Success, result.EmployeeNumber, result.Message);

        var status = result.Success ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity;
        var response = req.CreateResponse(status);
        await response.WriteAsJsonAsync(result, ct);
        return response;
    }

    // ─── Step 1: Read request ─────────────────────────────────────────────────

    private async Task<HaileyEmployee?> ReadRequestAsync(HttpRequestData req, CancellationToken ct)
    {
        try
        {
            var employee = await JsonSerializer.DeserializeAsync<HaileyEmployee>(req.Body, JsonOpts, ct);
            if (employee is null)
                logger.LogWarning("Deserialised employee payload is null.");
            else
                logger.LogDebug(
                    "Request deserialised successfully for EmploymentNumber={EmploymentNumber}",
                    employee.EmploymentNumber);

            return employee;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "JSON deserialisation failed: {Message}", ex.Message);
            return null;
        }
    }

    // ─── Step 2: Map HaileyEmployee → Quinyx UpdateEmployee ──────────────────
    // Field mapping per Attribute_Mapping.xlsx:
    //   firstName + lastName   → givenName + familyName
    //   companyEmail           → email
    //   workPhone              → phoneNo
    //   privatePhone           → cellPhone
    //   employmentNumber       → badgeNo
    //   dateOfJoining          → employedDate
    //   lastDayOfEmployment    → leaveDate
    //   personalIdentityNumber → socsecNo
    //   dateOfBirth            → dateOfBirth
    //   accountStatus          → active  (1 = active, 0 = inactive)
    //   streetAddress          → address1
    //   postalCode             → zip
    //   city                   → city
    //   country                → country
    //   iceName                → nextOfKind
    //   icePhone               → nextPhone
    //   customFieldsData       → additionalFields
    //   reportingTo            → reportingTo  (Logic App resolves managerEmployeeId → employmentNumber)
    //   extCostCentre          → extCostCentre (Logic App resolves costCenterId → costCenter.code)

    private UpdateEmployee MapToQuinyxEmployee(HaileyEmployee src)
    {
        logger.LogDebug(
            "Mapping HaileyEmployee {EmploymentNumber} to Quinyx UpdateEmployee", src.EmploymentNumber);

        var dest = new UpdateEmployee
        {
            badgeNo      = src.EmploymentNumber,
            givenName    = src.FirstName,
            familyName   = src.LastName,
            email        = src.CompanyEmail,
            phoneNo      = src.WorkPhone,
            cellPhone    = src.PrivatePhone,
            socsecNo     = src.PersonalIdentityNumber,
            address1     = src.StreetAddress,
            zip          = src.PostalCode,
            city         = src.City,
            country      = src.Country,
            nextOfKind   = src.IceName,
            nextPhone    = src.IcePhone,
            reportingTo  = src.ReportingTo,
            extCostCentre = src.ExtCostCentre,
        };

        // dateOfBirth is a string in Hailey; parse to DateTime for SOAP
        if (!string.IsNullOrWhiteSpace(src.DateOfBirth) &&
            DateTime.TryParse(src.DateOfBirth, out var dob))
        {
            dest.dateOfBirth          = dob;
            dest.dateOfBirthSpecified = true;
        }

        // dateOfJoining → employedDate
        if (src.DateOfJoining.HasValue)
        {
            dest.employedDate          = src.DateOfJoining.Value.ToDateTime(TimeOnly.MinValue);
            dest.employedDateSpecified = true;
        }

        // lastDayOfEmployment → leaveDate
        if (src.LastDayOfEmployment.HasValue)
        {
            dest.leaveDate          = src.LastDayOfEmployment.Value.ToDateTime(TimeOnly.MinValue);
            dest.leaveDateSpecified = true;
        }

        // accountStatus → active (Quinyx: 1 = active, 0 = inactive/passive)
        if (!string.IsNullOrWhiteSpace(src.AccountStatus))
        {
            dest.active          = string.Equals(src.AccountStatus, "active", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            dest.activeSpecified = true;
        }

        // customFieldsData → additionalFields (one entry per key-value pair)
        if (src.CustomFieldsData?.Count > 0)
        {
            dest.additionalFields = src.CustomFieldsData
                .SelectMany(kvp => kvp.Value
                    .Select(v => new AdditionalFieldData { key = kvp.Key, value = v }))
                .ToArray();
        }

        logger.LogDebug(
            "Mapping complete for badgeNo={BadgeNo} givenName={GivenName} familyName={FamilyName} email={Email}",
            dest.badgeNo, dest.givenName, dest.familyName, dest.email);

        return dest;
    }

    // ─── Step 3: Call the Quinyx service (delegates to SOAP trigger) ──────────

    private async Task<SyncResult> CallUpdateEmployeeAsync(UpdateEmployee employee, CancellationToken ct)
    {
        logger.LogInformation(
            "Calling QuinyxService.UpdateEmployeeAsync for badgeNo={BadgeNo}", employee.badgeNo);

        return await quinyxService.UpdateEmployeeAsync(employee, ct);
    }



}
