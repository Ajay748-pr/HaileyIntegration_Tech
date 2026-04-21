using System.Net.Http.Headers;
using System.Net.Http.Json;
using Azure.Identity;
using HaileyIntegration.Tech.Models;
using HaileyIntegration.Tech.Models.Dto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HaileyIntegration.Tech.Services.Downstream;

public sealed class IdentityProvisioningService(
    HttpClient http,
    IConfiguration config,
    ILogger<IdentityProvisioningService> logger) : IIdentityProvisioningService
{
    private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";

    public async Task<SyncResult> ProvisionAsync(CanonicalEmployee employee, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(employee.CompanyEmail))
        {
            return new SyncResult
            {
                Success = false,
                EmployeeNumber = employee.EmployeeNumber,
                TargetSystem = "ActiveDirectory",
                ErrorCode = "MISSING_EMAIL",
                Message = "CompanyEmail is required for identity provisioning."
            };
        }

        try
        {
            await SetBearerTokenAsync(ct);

            // Check if user already exists in AD via Graph API
            var existingUser = await FindUserByEmployeeNumberAsync(employee.EmployeeNumber, ct);

            if (existingUser is not null)
                return await UpdateUserAsync(existingUser, employee, ct);

            return await CreateUserAsync(employee, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Identity provisioning threw for employee {EmployeeNumber}", employee.EmployeeNumber);
            return new SyncResult
            {
                Success = false,
                EmployeeNumber = employee.EmployeeNumber,
                TargetSystem = "ActiveDirectory",
                ErrorCode = "EXCEPTION",
                Message = ex.Message
            };
        }
    }

    public async Task<SyncResult> DeprovisionAsync(CanonicalEmployee employee, CancellationToken ct = default)
    {
        try
        {
            await SetBearerTokenAsync(ct);
            var existingUser = await FindUserByEmployeeNumberAsync(employee.EmployeeNumber, ct);

            if (existingUser is null)
            {
                logger.LogWarning("Deprovision skipped — no AD user found for {EmployeeNumber}", employee.EmployeeNumber);
                return new SyncResult { Success = true, EmployeeNumber = employee.EmployeeNumber, TargetSystem = "ActiveDirectory", Message = "User not found — no action taken." };
            }

            // Disable account rather than delete (preserves audit trail)
            var patch = new { accountEnabled = false };
            var response = await http.PatchAsJsonAsync($"{GraphBaseUrl}/users/{existingUser}", patch, ct);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("AD account disabled for employee {EmployeeNumber}", employee.EmployeeNumber);
                return new SyncResult { Success = true, EmployeeNumber = employee.EmployeeNumber, TargetSystem = "ActiveDirectory" };
            }

            var error = await response.Content.ReadAsStringAsync(ct);
            return new SyncResult { Success = false, EmployeeNumber = employee.EmployeeNumber, TargetSystem = "ActiveDirectory", ErrorCode = ((int)response.StatusCode).ToString(), Message = error };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Identity deprovision threw for employee {EmployeeNumber}", employee.EmployeeNumber);
            return new SyncResult { Success = false, EmployeeNumber = employee.EmployeeNumber, TargetSystem = "ActiveDirectory", ErrorCode = "EXCEPTION", Message = ex.Message };
        }
    }

    private async Task<SyncResult> CreateUserAsync(CanonicalEmployee employee, CancellationToken ct)
    {
        var tenantDomain = config["EntraId:TenantDomain"]
            ?? throw new InvalidOperationException("EntraId:TenantDomain is not configured.");

        var upn = employee.CompanyEmail!.Contains('@')
            ? employee.CompanyEmail
            : $"{employee.CompanyEmail}@{tenantDomain}";

        var body = new
        {
            accountEnabled = true,
            displayName = employee.DisplayName,
            givenName = employee.FirstName,
            surname = employee.LastName,
            userPrincipalName = upn,
            mail = upn,
            employeeId = employee.EmployeeNumber,
            department = employee.DepartmentId?.ToString(),
            jobTitle = string.Join(", ", employee.TitleIds ?? []),
            mobilePhone = employee.WorkPhone,
            passwordProfile = new
            {
                forceChangePasswordNextSignIn = true,
                // Temporary password — employee must reset on first login
                password = GenerateTemporaryPassword()
            }
        };

        var response = await http.PostAsJsonAsync($"{GraphBaseUrl}/users", body, ct);

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("AD user created for employee {EmployeeNumber}", employee.EmployeeNumber);
            return new SyncResult { Success = true, EmployeeNumber = employee.EmployeeNumber, TargetSystem = "ActiveDirectory", Message = "User created" };
        }

        var error = await response.Content.ReadAsStringAsync(ct);
        logger.LogWarning("AD user creation failed for {EmployeeNumber}: {Status} {Error}", employee.EmployeeNumber, response.StatusCode, error);
        return new SyncResult { Success = false, EmployeeNumber = employee.EmployeeNumber, TargetSystem = "ActiveDirectory", ErrorCode = ((int)response.StatusCode).ToString(), Message = error };
    }

    private async Task<SyncResult> UpdateUserAsync(string userId, CanonicalEmployee employee, CancellationToken ct)
    {
        var patch = new
        {
            displayName = employee.DisplayName,
            givenName = employee.FirstName,
            surname = employee.LastName,
            department = employee.DepartmentId?.ToString(),
            mobilePhone = employee.WorkPhone,
            accountEnabled = employee.AccountStatus?.ToLowerInvariant() == "active"
        };

        var response = await http.PatchAsJsonAsync($"{GraphBaseUrl}/users/{userId}", patch, ct);

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("AD user updated for employee {EmployeeNumber}", employee.EmployeeNumber);
            return new SyncResult { Success = true, EmployeeNumber = employee.EmployeeNumber, TargetSystem = "ActiveDirectory", Message = "User updated" };
        }

        var error = await response.Content.ReadAsStringAsync(ct);
        return new SyncResult { Success = false, EmployeeNumber = employee.EmployeeNumber, TargetSystem = "ActiveDirectory", ErrorCode = ((int)response.StatusCode).ToString(), Message = error };
    }

    private async Task<string?> FindUserByEmployeeNumberAsync(string employeeNumber, CancellationToken ct)
    {
        var response = await http.GetAsync(
            $"{GraphBaseUrl}/users?$filter=employeeId eq '{employeeNumber}'&$select=id", ct);

        if (!response.IsSuccessStatusCode) return null;

        var result = await response.Content.ReadFromJsonAsync<GraphListResponse>(cancellationToken: ct);
        return result?.Value?.FirstOrDefault()?.Id;
    }

    private async Task SetBearerTokenAsync(CancellationToken ct)
    {
        var credential = new ManagedIdentityCredential();
        var token = await credential.GetTokenAsync(
            new Azure.Core.TokenRequestContext(["https://graph.microsoft.com/.default"]), ct);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
    }

    private static string GenerateTemporaryPassword()
    {
        // Satisfies AD default complexity: upper, lower, digit, special, length >= 8
        var guid = Guid.NewGuid().ToString("N")[..8].ToUpper();
        return $"Tmp@{guid}1!";
    }

    private sealed class GraphListResponse
    {
        public List<GraphUser>? Value { get; set; }
    }

    private sealed class GraphUser
    {
        public string? Id { get; set; }
    }
}
