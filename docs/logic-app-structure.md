# Logic App Structure — Hailey HR Integration

## Overview

A **Consumption Logic App** with System-Assigned Managed Identity, triggered every 6 hours.  
ARM template: `infrastructure/logic-app/workflow.json`

---

## Trigger

| Property  | Value         |
|-----------|---------------|
| Type      | Recurrence    |
| Frequency | Every 6 Hours |
| Timezone  | UTC           |

---

## Action Flow (Step by Step)

```
[TRIGGER] Every_6_Hours
        │
        ▼
[Step 1] Get Hailey API Key From Key Vault
        │  HTTP GET → https://{keyVaultName}.vault.azure.net/secrets/HaileyApiKey?api-version=7.4
        │  Auth: Managed Identity (audience: https://vault.azure.net)
        │  Output: body()?['value']  ← the raw API key string
        │
        ▼
[Step 2] Call Hailey Employee API
        │  HTTP GET → {haileyApiBaseUrl}/v1/employees
        │  Header: Authorization: Bearer {Step1.key}
        │  Output: JSON array of HaileyEmployee objects
        │
        ▼
[Step 3] Filter Last Updated Employees  (Azure Function)
        │  HTTP POST → {functionAppBaseUrl}/api/employees/filter
        │  Body: { "employees": [...], "windowHours": 6 }
        │  Output: { "employees": [...filtered], "filteredCount": N, "windowStart": "..." }
        │
        ▼
[Step 4] IF filteredCount > 0
        │
        ├─ TRUE → ForEach employee (concurrency: 10)
        │          │
        │          └─ IF accountStatus == "active" OR employmentStatus == "active"
        │               │
        │               ├─ TRUE (Active Employee) ─ all 4 run in PARALLEL:
        │               │   ├── POST /api/sync/primula       → PrimulaService
        │               │   ├── POST /api/sync/quinyx        → QuinyxService
        │               │   ├── POST /api/sync/learnify      → LearnifyService
        │               │   └── POST /api/identity/provision → IdentityProvisioningService
        │               │
        │               └─ FALSE (Terminated Employee):
        │                   ├── POST /api/identity/provision  → Disables AD account
        │                   └── POST /api/sync/primula        → Sends termination record
        │
        └─ FALSE → Compose "No updates in last 6 hours"
```

---

## Parameters

| Parameter          | Description                                       |
|--------------------|---------------------------------------------------|
| `keyVaultName`     | Azure Key Vault name (no URL — built in action)   |
| `haileyApiBaseUrl` | Hailey HR API base URL (e.g. `https://api.haileyhr.app`) |
| `functionAppBaseUrl` | Azure Function App base URL                     |
| `functionAppKey`   | Function App host key (stored securely in Key Vault) |

---

## Security

| Concern                      | Implementation                                           |
|------------------------------|----------------------------------------------------------|
| Hailey API key storage       | Azure Key Vault secret `HaileyApiKey`                   |
| Key Vault access             | System-Assigned Managed Identity with Key Vault Secrets User role |
| Azure Function authentication | Function-level key (`x-functions-key` header)          |
| No credentials in workflow   | All secrets retrieved at runtime; never hardcoded       |

---

## Key Vault RBAC Setup

After deploying the ARM template, assign the Managed Identity the following role:

```bash
az role assignment create \
  --assignee "<managed-identity-principal-id>" \
  --role "Key Vault Secrets User" \
  --scope "/subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.KeyVault/vaults/<kv-name>"
```

The `managedIdentityPrincipalId` output is available in the ARM template deployment outputs.

---

## ForEach Concurrency

The ForEach loop runs with `concurrency.repetitions = 10`, meaning up to 10 employees are processed simultaneously. Adjust based on downstream API rate limits:

- Primula / Quinyx / Learnify — check their throttling limits
- Microsoft Graph (identity) — default 10 req/s per tenant; 10 concurrent is safe

---

## Error Handling

- Each Azure Function returns `200 OK` on success and `422 Unprocessable Entity` on business errors (with a `SyncResult` JSON body including `errorCode` and `message`).
- Logic App continues processing remaining employees even if one fails — errors are captured per-employee in the Function response and logged to Application Insights.
- For infrastructure failures (Key Vault down, Hailey API unavailable), the Logic App run fails and Azure Monitor alerts.
