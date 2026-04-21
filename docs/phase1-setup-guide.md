# Phase 1 Setup Guide — Hailey API + Key Vault Only

You have: **Hailey HR API** + **Azure Key Vault**  
You need to deploy: **1 Azure Function** + **1 Logic App**

---

## Step 1 — Rotate the Key Vault Secret

> If you shared the API key anywhere (chat, email, Teams), regenerate it immediately.

1. Go to **Key Vault → Secrets → HaileyApiKey**
2. Click **New Version** and paste the new key
3. Disable the old version

---

## Step 2 — Deploy the Azure Function App

### 2a. Create the Function App in Azure Portal

| Setting | Value |
|---|---|
| Runtime | .NET 8 Isolated |
| OS | Windows or Linux |
| Plan | Consumption (Y1) |
| Region | Same as Key Vault |

### 2b. Publish from Visual Studio / CLI

```bash
# From the repo root
dotnet publish HaileyIntegration_Tech/HaileyIntegration_Tech.csproj -c Release

# Or use Azure Functions Core Tools
cd HaileyIntegration_Tech
func azure functionapp publish <your-function-app-name>
```

### 2c. Set Application Settings on the Function App

In Azure Portal → Function App → Configuration → Application Settings, add:

| Name | Value |
|---|---|
| `Primula:BaseUrl` | `https://placeholder.invalid/` ← dummy for now, not called in Phase 1 |
| `Quinyx:BaseUrl` | `https://placeholder.invalid/` ← dummy for now |
| `Learnify:BaseUrl` | `https://placeholder.invalid/` ← dummy for now |
| `EntraId:TenantDomain` | `placeholder.onmicrosoft.com` ← dummy for now |

> These settings are required by `Program.cs` startup validation. Phase 1 doesn't call them — they just need to be present to pass the startup check.

### 2d. Get the Function App Host Key

Azure Portal → Function App → **App Keys** → **default** → copy the value.  
You'll need this for the Logic App parameter `functionAppKey`.

---

## Step 3 — Deploy the Logic App (Phase 1)

Use `infrastructure/logic-app/workflow-phase1.json`.

### Option A — Azure Portal (Custom Deployment)

1. Azure Portal → **Deploy a custom template**
2. Upload `workflow-phase1.json`
3. Fill in parameters:

| Parameter | Example value |
|---|---|
| `logicAppName` | `la-hailey-hr-sync` |
| `keyVaultName` | `kv-tekniska-hr` (just the name, not the URL) |
| `haileyApiBaseUrl` | `https://api.haileyhr.app` (your actual Hailey base URL) |
| `functionAppBaseUrl` | `https://<your-funcapp>.azurewebsites.net` |
| `functionAppKey` | *(host key from Step 2d)* |

### Option B — Azure CLI

```bash
az deployment group create \
  --resource-group <your-rg> \
  --template-file infrastructure/logic-app/workflow-phase1.json \
  --parameters \
      logicAppName="la-hailey-hr-sync" \
      keyVaultName="<your-kv-name>" \
      haileyApiBaseUrl="https://api.haileyhr.app" \
      functionAppBaseUrl="https://<funcapp>.azurewebsites.net" \
      functionAppKey="<host-key>"
```

---

## Step 4 — Grant Key Vault Access to the Logic App

After deployment, copy the **Managed Identity Principal ID** from the ARM output, then run:

```bash
az role assignment create \
  --assignee "<managed-identity-principal-id>" \
  --role "Key Vault Secrets User" \
  --scope "/subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.KeyVault/vaults/<kv-name>"
```

Or in Azure Portal: **Key Vault → Access Control (IAM) → Add → Key Vault Secrets User → select the Logic App identity**.

---

## Step 5 — Test the Flow

1. Azure Portal → Logic App → **Run Trigger → Run Now**
2. Click the run to inspect each step:
   - Step 1: Key Vault response should show `"value": "***"` (masked)
   - Step 2: Hailey API response should be a JSON array
   - Step 3: Filter function should return `filteredCount` and `employees`
   - Step 4: Compose outputs showing employee records (or "No updates" message)

### Verify FilterEmployees Function directly

```bash
curl -X POST https://<funcapp>.azurewebsites.net/api/employees/filter \
  -H "x-functions-key: <host-key>" \
  -H "Content-Type: application/json" \
  -d '{
    "employees": [...paste Hailey API sample response here...],
    "windowHours": 6
  }'
```

Expected response:
```json
{
  "employees": [...],
  "totalReceived": 10,
  "filteredCount": 2,
  "windowStart": "2026-04-21T00:00:00Z",
  "windowEnd": "2026-04-21T06:00:00Z"
}
```

---

## What Phase 1 Does NOT Do Yet

| Feature | Status | Added in |
|---|---|---|
| Sync to Primula | Not called | Phase 2 |
| Sync to Quinyx | Not called | Phase 2 |
| Sync to Learnify | Not called | Phase 2 |
| AD/Entra ID provisioning | Not called | Phase 2 |

The downstream Azure Functions (`SyncToPrimula`, `SyncToQuinyx`, etc.) are deployed but not wired into the Logic App yet. They sit idle until Phase 2.

---

## What Happens in Each Logic App Run (Phase 1)

```
Every 6 hours:
  ✓ Retrieve HaileyApiKey from Key Vault (Managed Identity)
  ✓ Call Hailey /v1/employees API
  ✓ POST to FilterEmployees Function → get employees updated in last 6h
  IF updated employees exist:
    ✓ Log run summary (count, window, employee IDs) → visible in Logic App run history
    ✓ Loop through each employee → log individual record
  ELSE:
    ✓ Log "No updates" → run succeeds silently
```

All outputs are visible in **Logic App run history** in the Azure Portal.
