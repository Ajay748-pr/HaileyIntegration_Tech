# Epics & Stories — Tekniska Museet HR Integration (Hailey HR)

> Constraint: Only Azure Logic Apps and Azure Functions are permitted.  
> Hailey HR is the System of Record. Employee Number is the master integration key.  
> Integration runs every 6 hours via Logic App recurrence trigger.

---

## Epic 1 — Infrastructure & Security Foundation

**Goal:** Establish the secure Azure infrastructure: Function App, Key Vault access via Managed Identity, and application configuration — before any integration logic is written.

**Why first:** All downstream epics depend on secrets management and a deployable Function App. Getting this right prevents security debt.

---

### Story 1.1 — Deploy Azure Function App with Managed Identity

**As a** platform engineer,  
**I want** an Azure Function App (.NET 8 isolated) deployed with a System-Assigned Managed Identity,  
**so that** it can authenticate to Azure services without storing credentials in code or config.

**Acceptance criteria:**
- [ ] Function App targets `dotnet-isolated` runtime, .NET 8
- [ ] System-Assigned Managed Identity enabled on the Function App
- [ ] Application Insights connected via `APPLICATIONINSIGHTS_CONNECTION_STRING`
- [ ] Function App accessible only via function-level keys (no anonymous triggers)

---

### Story 1.2 — Configure Azure Key Vault and Store Secrets

**As a** platform engineer,  
**I want** an Azure Key Vault provisioned with all integration secrets,  
**so that** no credential is stored in source code or app settings in plain text.

**Acceptance criteria:**
- [ ] Key Vault created with `HaileyApiKey` secret populated
- [ ] Logic App Managed Identity assigned `Key Vault Secrets User` role on the vault
- [ ] Function App reads downstream API URLs from app settings (not Key Vault — not secrets)
- [ ] `EntraId:TenantDomain` set in Function App configuration

**Secrets to store in Key Vault:**

| Secret Name        | Description                              |
|--------------------|------------------------------------------|
| `HaileyApiKey`     | Bearer token for Hailey Employee API     |
| `PrimulaApiKey`    | API key for Primula (if required)        |
| `QuinyxApiKey`     | API key for Quinyx (if required)         |
| `LearnifyApiKey`   | API key for Learnify (if required)       |

---

### Story 1.3 — Deploy Logic App with Managed Identity and Recurrence Trigger

**As a** platform engineer,  
**I want** an Azure Consumption Logic App deployed using the ARM template in `infrastructure/logic-app/workflow.json`,  
**so that** the 6-hour integration schedule is operational from day one.

**Acceptance criteria:**
- [ ] Logic App deployed via ARM template with all parameters filled
- [ ] Recurrence trigger set to every 6 hours, UTC
- [ ] System-Assigned Managed Identity enabled on the Logic App
- [ ] Logic App Managed Identity has `Key Vault Secrets User` role (see Story 1.2)
- [ ] Logic App visible and triggerable in Azure Portal

---

### Story 1.4 — Configure Application Settings for All Downstream Endpoints

**As a** developer,  
**I want** all downstream API base URLs configured as Function App application settings,  
**so that** each environment (dev/staging/prod) points to the correct system endpoints.

**Acceptance criteria:**
- [ ] `Primula:BaseUrl`, `Quinyx:BaseUrl`, `Learnify:BaseUrl` set in all environments
- [ ] `EntraId:TenantDomain` configured per environment
- [ ] Settings validated at startup (Program.cs throws `InvalidOperationException` if missing)
- [ ] No hardcoded URLs anywhere in source code

---

## Epic 2 — Hailey HR Data Ingestion & Filtering

**Goal:** Retrieve all employees from the Hailey HR API and isolate only those updated within the last 6 hours, producing a clean filtered list for downstream processing.

**Why:** Hailey's API returns the full employee dataset. Sending all records to every downstream system on every run would cause unnecessary load, duplicate processing, and data integrity risk. Filtering to `lastUpdated` delta is the core data ingestion logic.

---

### Story 2.1 — Logic App: Retrieve API Key and Call Hailey Employee API

**As a** Logic App workflow,  
**I want** to retrieve the `HaileyApiKey` from Key Vault at runtime and use it to call the Hailey `/v1/employees` endpoint,  
**so that** a fresh list of all employees is fetched on every run without any stored credentials.

**Acceptance criteria:**
- [ ] Step 1 in Logic App calls Key Vault REST API with Managed Identity auth
- [ ] Step 2 calls Hailey API with `Authorization: Bearer {key}` header
- [ ] Successful response is a JSON array of employee objects matching the `HaileyEmployee` model
- [ ] If Key Vault call fails → Logic App run fails with clear error (monitored by alert)
- [ ] If Hailey API returns non-2xx → Logic App run fails with alert fired

---

### Story 2.2 — Implement `FilterEmployeesFunction` Azure Function

**As a** Logic App workflow,  
**I want** to POST the full employee list to `FilterEmployeesFunction`,  
**so that** only employees with `lastUpdated > (now - 6 hours - 5 min buffer)` are returned.

**Acceptance criteria:**
- [ ] `POST /api/employees/filter` accepts `FilterEmployeesRequest` (employees array + windowHours)
- [ ] Returns `FilterEmployeesResponse` with filtered employees, counts, and window bounds
- [ ] 5-minute buffer applied to avoid missing edge-case updates at the boundary
- [ ] `windowHours` defaults to 6 if not provided
- [ ] `referenceTime` override supported for backfill/replay scenarios
- [ ] Returns `400 Bad Request` for malformed body
- [ ] Logs total received vs. filtered count at Information level

---

### Story 2.3 — Implement `EmployeeMappingService` (Hailey → Canonical Model)

**As a** developer,  
**I want** each `HaileyEmployee` mapped to a `CanonicalEmployee` before downstream dispatch,  
**so that** downstream functions deal with a consistent, system-agnostic model.

**Acceptance criteria:**
- [ ] All fields from `HaileyEmployee` correctly mapped to `CanonicalEmployee`
- [ ] `EmploymentNumber` validated as mandatory; throws `InvalidOperationException` if missing
- [ ] `DisplayName` generated as `"{FirstName} {LastName} ({EmploymentNumber})"` to guarantee AD uniqueness (per SDD 5.6 constraint)
- [ ] `ChangeType` enum correctly inferred from `employmentStatus` / `accountStatus`
- [ ] Unit tests covering: normal mapping, missing employment number, terminated employee, display name uniqueness

---

### Story 2.4 — Logic App: Conditional Branch on Filtered Count

**As a** Logic App workflow,  
**I want** to branch on whether any employees were returned by the filter function,  
**so that** no downstream calls are made when nothing has changed (avoiding unnecessary API calls and noise in logs).

**Acceptance criteria:**
- [ ] `IF filteredCount > 0` → proceed to ForEach; `ELSE` → log "No updates" Compose action
- [ ] ForEach concurrency set to 10 (configurable without redeployment)
- [ ] Logic App run marked `Succeeded` in both branches (not skipped/failed when no updates)

---

## Epic 3 — Downstream System Synchronization

**Goal:** For each updated employee, synchronize the relevant data to Primula (payroll), Quinyx (workforce), and Learnify (LMS). Active and terminated employees follow different paths.

---

### Story 3.1 — Implement `SyncToPrimulaFunction`

**As a** Logic App workflow,  
**I want** to POST each updated employee to `SyncToPrimulaFunction`,  
**so that** Primula always reflects the latest employment, contract, and payroll data from Hailey HR.

**Fields synced to Primula:** `employmentNumber`, name, `employmentType`, `employmentStatus`, `startDate`, `endDate`, `lastWorkingDay`, `scopePercentage`, `scopeHours`, `vacationDays`, `fixedTermType`, `endOfFixedTerm`, `costCenterId`, and banking details (`bankName`, `clearingNumber`, `accountNumber`, `iban`, `bic`).

**Acceptance criteria:**
- [ ] `POST /api/sync/primula` maps `HaileyEmployee` → canonical → Primula payload
- [ ] Termination records (terminated/resigned/retired) are sent with correct `employmentStatus`
- [ ] `SyncResult` returned with `success`, `employeeNumber`, `targetSystem`, and error details on failure
- [ ] HTTP errors from Primula are captured in `SyncResult` (not thrown as exceptions)
- [ ] Successful sync logged at Information; failures logged at Warning with status code

---

### Story 3.2 — Implement `SyncToQuinyxFunction`

**As a** Logic App workflow,  
**I want** to POST each active updated employee to `SyncToQuinyxFunction`,  
**so that** Quinyx always has current role, schedule scope, and team assignment data from Hailey HR.

**Fields synced to Quinyx:** `employmentNumber`, name, `employmentType`, `employmentStatus`, `startDate`, `endDate`, `scopePercentage`, `departmentId`, `locationId`, `teamIds`, `titleIds`.

**Acceptance criteria:**
- [ ] `POST /api/sync/quinyx` endpoint implemented and tested
- [ ] Employee Number used as the matching key in Quinyx (per architecture requirement)
- [ ] Only active employees are sent (terminated employees skip Quinyx sync — handled in Logic App condition)
- [ ] Errors handled consistently with `SyncResult` contract

---

### Story 3.3 — Implement `SyncToLearnifyFunction`

**As a** Logic App workflow,  
**I want** to POST each active updated employee to `SyncToLearnifyFunction`,  
**so that** Learnify user accounts remain in sync with Hailey HR, enabling correct training assignment.

**Fields synced to Learnify:** `employmentNumber`, name, `companyEmail`, `departmentId`, `employmentStatus`, `startDate`.

**Acceptance criteria:**
- [ ] `POST /api/sync/learnify` endpoint implemented
- [ ] `companyEmail` used as the Learnify user identifier
- [ ] New employees create a Learnify user; existing employees update it
- [ ] Errors handled consistently with `SyncResult` contract

---

### Story 3.4 — Logic App: Parallel Downstream Dispatch with Active/Terminated Branch

**As a** Logic App workflow,  
**I want** to dispatch to all downstream systems in parallel for active employees, and only to Primula + identity for terminated employees,  
**so that** the integration is fast and terminated employees do not get unnecessarily re-provisioned in Quinyx or Learnify.

**Acceptance criteria:**
- [ ] Active path: Primula + Quinyx + Learnify + Identity run **in parallel** (no sequential dependency)
- [ ] Terminated path: only Primula (termination record) + Identity deprovision
- [ ] Condition evaluates `accountStatus == "active"` OR `employmentStatus == "active"` (case-insensitive)
- [ ] Individual downstream failures do not abort other parallel branches

---

## Epic 4 — Identity Provisioning (Active Directory & Entra ID)

**Goal:** Automatically create, update, or disable Active Directory accounts for employees using Microsoft Graph API, triggered by Hailey HR lifecycle events.

**Why separate epic:** Identity provisioning has unique constraints (AD duplicate name rule, Graph API auth, Entra Connect sync chain) and is the highest-risk integration — a mistake can lock out employees or leave ghost accounts.

---

### Story 4.1 — Implement `ProvisionIdentityFunction` — Create / Update AD User

**As a** Logic App workflow,  
**I want** to POST a new or updated employee to `ProvisionIdentityFunction`,  
**so that** Active Directory accounts are automatically created or updated without manual IT intervention.

**Acceptance criteria:**
- [ ] `POST /api/identity/provision` accepts a `HaileyEmployee` payload
- [ ] `IdentityProvisioningService` uses Managed Identity (`ManagedIdentityCredential`) to get a Graph API token
- [ ] Checks if user already exists in AD via `GET /users?$filter=employeeId eq '{employeeNumber}'`
- [ ] **Create path:** POST to Graph `/users` with `accountEnabled=true`, `forceChangePasswordNextSignIn=true`
- [ ] **Update path:** PATCH to Graph `/users/{id}` with changed fields only
- [ ] `DisplayName` guaranteed unique via `"{FirstName} {LastName} ({EmployeeNumber})"` format (SDD constraint)
- [ ] `userPrincipalName` derived from `companyEmail`
- [ ] Returns `400` if `companyEmail` is missing (required for AD UPN)

---

### Story 4.2 — Implement Identity Deprovisioning (Account Disable on Termination)

**As a** Logic App workflow,  
**I want** to disable the AD account when an employee is terminated,  
**so that** access to all M365 services (Teams, SharePoint, Learnify, Email) is revoked immediately without deleting audit history.

**Acceptance criteria:**
- [ ] Termination detected via `accountStatus` or `employmentStatus` values: `terminated`, `resigned`, `retired`, `inactive`
- [ ] Account is **disabled** (PATCH `accountEnabled=false`) — NOT deleted (preserves audit trail)
- [ ] If no AD account found → log warning, return success (idempotent)
- [ ] Deprovisioning result logged with employee number and timestamp

---

### Story 4.3 — Managed Identity Permission Setup for Graph API

**As a** platform engineer,  
**I want** the Function App's Managed Identity to have the minimum required Microsoft Graph permissions,  
**so that** the identity provisioning function can create and update AD users securely.

**Required Graph API permissions (Application, not delegated):**

| Permission                   | Purpose                             |
|------------------------------|-------------------------------------|
| `User.ReadWrite.All`         | Create and update AD users          |
| `Directory.ReadWrite.All`    | Read directory for duplicate checks |

**Acceptance criteria:**
- [ ] Graph permissions assigned to Function App Managed Identity via `az ad app permission add` or Entra portal
- [ ] Admin consent granted for all application permissions
- [ ] Permissions documented in `infrastructure/` alongside the ARM template
- [ ] Local dev uses a Service Principal with the same permissions (not production Managed Identity)

---

### Story 4.4 — Entra Connect Sync Validation

**As a** platform engineer,  
**I want** to validate that Entra Connect picks up AD accounts created by the provisioning function,  
**so that** new hires automatically get access to Email, Teams, SharePoint, and Learnify through Entra ID.

**Acceptance criteria:**
- [ ] AD account created by `ProvisionIdentityFunction` appears in Entra ID within the Entra Connect sync cycle
- [ ] User has access to: Exchange Online (Email), Microsoft Teams, SharePoint, Learnify
- [ ] Test with a dummy employee record; clean up after validation
- [ ] Sync delay documented (default Entra Connect cycle: 30 min)

---

## Epic 5 — Monitoring, Observability & Alerting

**Goal:** Ensure every integration failure is detected, logged, and alerted on within minutes, giving the operations team full visibility into every Logic App run and Function execution.

---

### Story 5.1 — Application Insights Integration and Structured Logging

**As a** developer,  
**I want** all Azure Functions to emit structured logs to Application Insights,  
**so that** integration failures, filtered counts, and sync results are queryable in Log Analytics.

**Acceptance criteria:**
- [ ] `APPLICATIONINSIGHTS_CONNECTION_STRING` configured on Function App
- [ ] All `ILogger` calls use structured logging (no string interpolation in log messages)
- [ ] Key log events: filter result (count), each sync result (success/failure per system), identity provision outcome
- [ ] `SyncResult` failures logged at `Warning` with `employeeNumber`, `targetSystem`, `errorCode`

---

### Story 5.2 — Azure Monitor Alerts for Logic App Failures

**As an** operations engineer,  
**I want** Azure Monitor alerts configured for Logic App run failures,  
**so that** the team is notified within 5 minutes if the 6-hourly sync fails.

**Acceptance criteria:**
- [ ] Alert rule on `Microsoft.Logic/workflows` metric `RunsFailed > 0`
- [ ] Alert fires to an Action Group (email + Teams webhook)
- [ ] Alert suppressed for "No updates" runs (not a failure condition)
- [ ] Separate alert for Hailey API call returning non-2xx (Step 2 failure)

---

### Story 5.3 — Log Analytics Dashboard for Integration Health

**As an** operations engineer,  
**I want** a Log Analytics workbook showing integration run summary per 6-hour window,  
**so that** I can monitor trends and identify recurring failures without digging through raw logs.

**Key queries:**
- Employees filtered per run (trend over 7 days)
- Sync success/failure rate per downstream system
- Identity provisioning outcomes (created / updated / disabled / failed)
- Top error codes by frequency

**Acceptance criteria:**
- [ ] Workbook created in Azure Monitor with above panels
- [ ] Workbook pinned to resource group dashboard
- [ ] Data available within 5 minutes of each Logic App run

---

## Story Mapping Summary

| Epic | Stories | Priority |
|------|---------|----------|
| Epic 1 — Infrastructure & Security | 1.1, 1.2, 1.3, 1.4 | **Sprint 1** |
| Epic 2 — Data Ingestion & Filtering | 2.1, 2.2, 2.3, 2.4 | **Sprint 1** |
| Epic 3 — Downstream Sync | 3.1, 3.2, 3.3, 3.4 | **Sprint 2** |
| Epic 4 — Identity Provisioning | 4.1, 4.2, 4.3, 4.4 | **Sprint 2** |
| Epic 5 — Monitoring | 5.1, 5.2, 5.3 | **Sprint 3** |
