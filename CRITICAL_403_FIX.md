# CRITICAL: Fix 403 Forbidden Error on Production

## Problem
Application pool crashes with "fatal communication error" and 403 Forbidden error.

## Root Cause
Database migration was NOT run on production database. Background services try to access tables that don't exist, causing the application to crash on startup.

## IMMEDIATE FIX (Do this NOW)

### Step 1: Run Database Migration on Production

Open PowerShell in your project directory and run:

```powershell
dotnet ef database update --project Infrastructure --startup-project Web --connection "Server=db42073.public.databaseasp.net;Database=db42073;User Id=db42073;Password=9h+KT7d?2-Xx;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
```

This will create the missing tables:
- AuditLogs
- ReconciliationReports
- SecretRotationHistory
- SecurityAuditLogs
- TransactionMismatches

### Step 2: Verify Migration Success

Check that the migration completed successfully. You should see:
```
Applying migration '20260223134204_PaymentOrchestrationComplete'.
Done.
```

### Step 3: Deploy Updated Code

The background services have been updated to be more resilient. Deploy the changes:

```powershell
git add Application/Services/ReconciliationBackgroundService.cs
git add Application/Services/SecretRotationBackgroundService.cs
git add Web/Controllers/HomeController.cs
git commit -m "Fix: Make background services resilient and fix HomeController redirect"
git push origin main
```

### Step 4: Wait for Deployment

MonsterASP.net will automatically deploy from GitHub. Wait 2-3 minutes.

### Step 5: Test the Application

Visit: http://fintechapi.runasp.net/

You should be redirected to Swagger UI.

## What Was Fixed

1. **Background Services**: Now wait 5 minutes after startup before accessing the database, giving time for initialization
2. **TaskCanceledException Handling**: Properly catches cancellation exceptions to prevent crash loops
3. **HomeController**: Fixed redirect loop (was redirecting to itself, now redirects to /swagger)

## If Still Getting 403 Error

### Check Server Logs
1. Log into MonsterASP.net control panel
2. Go to your site's file manager
3. Navigate to `logs/` folder
4. Open the latest `stdout_*.log` file
5. Look for the actual error message

### Common Issues

**Issue**: "Cannot open database" error
**Fix**: Verify connection string in MonsterASP.net configuration matches:
```
Server=db42073.public.databaseasp.net;Database=db42073;User Id=db42073;Password=9h+KT7d?2-Xx;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

**Issue**: "SecureData.json not found"
**Fix**: Ensure `SecureData.json` exists on the server with JWT configuration:
```json
{
  "Jwt": {
    "KEY": "your-secret-key-here-at-least-32-characters-long",
    "ISSUER": "FintechAPI",
    "AUDIENCE": "FintechAPIUsers"
  }
}
```

**Issue**: "Table does not exist" errors
**Fix**: Migration wasn't run. Go back to Step 1.

## Verification Checklist

- [ ] Database migration completed successfully
- [ ] All 5 new tables exist in production database
- [ ] Code changes pushed to GitHub
- [ ] MonsterASP.net deployed the changes
- [ ] Application loads without 403 error
- [ ] Swagger UI is accessible
- [ ] No errors in stdout logs

## Next Steps After Fix

Once the application is running:

1. Test API endpoints through Swagger
2. Verify background services are running (check logs)
3. Test payment provider integrations
4. Run reconciliation manually to verify it works

## Support

If you're still getting errors after following these steps, check the stdout logs and share the exact error message.
