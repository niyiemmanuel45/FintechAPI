# MonsterASP.net Deployment - Quick Fix Guide

## Your Current Issue
- **Error**: 403 Forbidden
- **Server Log**: Process terminated unexpectedly (Exit code 0xffffffff)
- **Cause**: Application is crashing on startup

---

## IMMEDIATE ACTIONS (Do These First!)

### 1. Enable Detailed Error Logging

**Create/Update web.config in your deployment:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\Web.dll"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
      <httpErrors errorMode="Detailed" />
    </system.webServer>
  </location>
</configuration>
```

**Create a `logs` folder** in your deployment directory with write permissions.

### 2. Check the Logs

After deploying the updated web.config:
1. Try to access your site
2. Go to the `logs` folder
3. Open the newest `stdout_*.log` file
4. **This will show you the EXACT error**

---

## MOST LIKELY CAUSES (Based on Your Setup)

### Cause #1: Database Migration Not Run ⚠️ MOST LIKELY

**Problem:** The new tables (AuditLogs, ReconciliationReports, etc.) don't exist in production.

**Solution:**

**Option A - Run from your local machine:**
```bash
dotnet ef database update --project Infrastructure --startup-project Web --connection "Server=db42073.public.databaseasp.net;Database=db42073;User Id=db42073;Password=9h+KT7d?2-Xx;Encrypt=True;TrustServerCertificate=True;"
```

**Option B - Generate SQL script:**
```bash
dotnet ef migrations script --project Infrastructure --startup-project Web --output migration.sql
```
Then run the SQL script on your production database.

### Cause #2: Missing SecureData.json

**Problem:** SecureData.json is not deployed or has wrong settings.

**Solution:** Ensure SecureData.json exists in your deployment with:
```json
{
  "ConnectionStrings": {
    "Remote": "Server=db42073.public.databaseasp.net;Database=db42073;User Id=db42073;Password=9h+KT7d?2-Xx;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  },
  "Jwt": {
    "KEY": "YourSuperSecretJwtKeyThatIsAtLeast32CharactersLongForHS256Algorithm",
    "ISSUER": "FintechAPI",
    "AUDIENCE": "FintechAPIUsers"
  },
  "Stripe": {
    "SecretKey": "Your_Stripe_Secret_Key_Here"
  }
}
```

### Cause #3: .NET 8.0 Runtime Not Installed

**Problem:** MonsterASP.net doesn't have .NET 8.0 runtime.

**Solution:** Deploy as self-contained:
```bash
dotnet publish Web/Web.csproj -c Release -r win-x64 --self-contained true -o ./publish
```

This includes the .NET runtime in your deployment (larger file size but guaranteed to work).

---

## STEP-BY-STEP FIX PROCEDURE

### Step 1: Redeploy with Proper Configuration

1. **Run the deployment script:**
   ```bash
   deploy-to-production.bat
   ```

2. **Update SecureData.json** in the `publish` folder with production settings

3. **Upload everything** from `publish` folder to MonsterASP.net

### Step 2: Run Database Migration

**Before the app can start, you MUST run the migration:**

```bash
dotnet ef database update --project Infrastructure --startup-project Web
```

Or use the SQL script method if you can't run EF commands.

### Step 3: Configure IIS Application Pool

In MonsterASP.net control panel:
- .NET CLR Version: **No Managed Code**
- Managed Pipeline Mode: **Integrated**
- Enable 32-Bit Applications: **False**

### Step 4: Check Logs

1. Access your site
2. If it still fails, check `logs/stdout_*.log`
3. The log will show the exact error

---

## Common Errors in Logs & Solutions

### Error: "Invalid object name 'AuditLogs'"
**Solution:** Run database migrations (see Cause #1 above)

### Error: "Value cannot be null. (Parameter 's')" at Encoding.GetBytes
**Solution:** JWT KEY is missing in SecureData.json

### Error: "A network-related or instance-specific error"
**Solution:** Database connection string is wrong or database is not accessible

### Error: "Could not load file or assembly"
**Solution:** Missing DLLs - redeploy with self-contained publish

---

## Quick Test Commands

### Test Database Connection from Local Machine:
```bash
sqlcmd -S db42073.public.databaseasp.net -U db42073 -P "9h+KT7d?2-Xx" -Q "SELECT @@VERSION"
```

### Check if Migration is Needed:
```bash
dotnet ef migrations list --project Infrastructure --startup-project Web
```

### Generate Migration SQL Script:
```bash
dotnet ef migrations script --project Infrastructure --startup-project Web --idempotent --output migration.sql
```

---

## CRITICAL: Database Migration SQL

If you can't run `dotnet ef database update`, here's what the migration creates:

**Tables Created:**
- AuditLogs
- ReconciliationReports  
- SecretRotationHistory
- SecurityAuditLogs
- TransactionMismatches

**You can run this manually in SQL Server Management Studio or MonsterASP.net SQL panel.**

To get the SQL script:
```bash
dotnet ef migrations script --project Infrastructure --startup-project Web --output migration.sql
```

---

## What to Send Me for Help

If it still doesn't work, send me:

1. **Contents of `logs/stdout_*.log`** (the newest one)
2. **Screenshot of MonsterASP.net application pool settings**
3. **Confirmation that database migration was run**
4. **List of files in your deployment folder**

The stdout log will tell us exactly what's wrong!

---

## Expected Behavior When Working

When the app starts successfully, you should see:
- No 403 error
- Swagger UI at `/swagger` (if enabled)
- API endpoints responding
- Background services running (check logs for "Reconciliation Background Service started")

---

## Emergency Workaround

If you need the app running ASAP and background services are causing issues:

**Temporarily disable background services in Program.cs:**
```csharp
// Comment these out:
// builder.Services.AddHostedService<Application.Services.ReconciliationBackgroundService>();
// builder.Services.AddHostedService<Application.Services.SecretRotationBackgroundService>();
```

Then redeploy. This will get the API working while you troubleshoot the background services.

---

## Summary

**Most likely issue:** Database migration not run on production database.

**Quick fix:**
1. Run `dotnet ef database update` on production database
2. Ensure SecureData.json is deployed with correct settings
3. Deploy as self-contained if .NET 8.0 runtime is missing
4. Check stdout logs for exact error

**The logs will tell you exactly what's wrong!**
