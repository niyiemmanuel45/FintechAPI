# GitHub Deployment to MonsterASP.net

## Overview
This guide covers deploying your FintechAPI from GitHub to MonsterASP.net using their GitHub integration.

---

## CRITICAL: Pre-Deployment Checklist

### 1. Files That MUST NOT Be in GitHub
These files contain secrets and should be in `.gitignore`:

- ❌ `SecureData.json` (contains database passwords, API keys)
- ❌ `appsettings.Production.json` (if it has secrets)
- ❌ Any files with connection strings or API keys

### 2. Files That MUST Be in GitHub
These files are required for deployment:

- ✅ `web.config` (IIS configuration)
- ✅ `Web.csproj` (project file)
- ✅ All `.cs` source files
- ✅ `appsettings.json` (without secrets)
- ✅ All migration files in `Infrastructure/Migrations/`

---

## Step 1: Prepare Your Repository

### Update .gitignore

Ensure your `.gitignore` includes:

```gitignore
# Secrets and sensitive data
SecureData.json
appsettings.Production.json
*.user
*.suo

# Build results
[Bb]in/
[Oo]bj/
[Pp]ublish/

# Logs
logs/
*.log

# User-specific files
*.rsuser
*.suo
*.user
*.userosscache
*.sln.docstates
```

### Commit Required Files

Make sure these are committed:

```bash
git add Web/web.config
git add Web/Web.csproj
git add Infrastructure/Migrations/
git add Application/
git add Core/
git add DEPLOYMENT_TROUBLESHOOTING.md
git add MONSTERASP_QUICK_FIX.md
git commit -m "Add deployment configuration and guides"
git push origin main
```

---

## Step 2: Configure MonsterASP.net GitHub Deployment

### In MonsterASP.net Control Panel:

1. **Navigate to GitHub Deploy section**
2. **Connect your GitHub repository**
   - Repository URL: `https://github.com/yourusername/FintechAPI`
   - Branch: `main` (or your deployment branch)

3. **Configure Build Settings:**
   - **Project Path**: `Web/Web.csproj`
   - **Build Configuration**: `Release`
   - **Target Framework**: `net8.0`
   - **Runtime**: `win-x64` (self-contained recommended)

4. **Build Command** (if customizable):
   ```bash
   dotnet publish Web/Web.csproj -c Release -r win-x64 --self-contained true -o ./publish
   ```

---

## Step 3: Add SecureData.json Manually

Since `SecureData.json` is NOT in GitHub (for security), you need to add it manually:

### Option A: Through MonsterASP.net File Manager

1. After deployment, go to File Manager
2. Navigate to your site root
3. Create `SecureData.json` with production settings:

```json
{
  "ConnectionStrings": {
    "Remote": "Server=db42073.public.databaseasp.net;Database=db42073;User Id=db42073;Password=9h+KT7d?2-Xx;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  },
  "Jwt": {
    "KEY": "YourProductionJwtKeyAtLeast32CharactersLongForHS256",
    "ISSUER": "FintechAPI",
    "AUDIENCE": "FintechAPIUsers"
  },
  "Stripe": {
    "SecretKey": "sk_live_your_production_stripe_key"
  },
  "PaymentProviders": {
    "Paystack": {
      "SecretKey": "sk_live_your_paystack_key",
      "PublicKey": "pk_live_your_paystack_public_key",
      "WebhookSecret": "your_paystack_webhook_secret",
      "BaseUrl": "https://api.paystack.co"
    },
    "Flutterwave": {
      "SecretKey": "FLWSECK-your_flutterwave_key",
      "PublicKey": "FLWPUBK-your_flutterwave_public_key",
      "WebhookSecret": "your_flutterwave_webhook_secret",
      "BaseUrl": "https://api.flutterwave.com/v3",
      "RedirectUrl": "https://yoursite.monsterasp.net/payment/callback"
    },
    "Remita": {
      "MerchantId": "your_remita_merchant_id",
      "ApiKey": "your_remita_api_key",
      "ServiceTypeId": "your_service_type_id",
      "BaseUrl": "https://login.remita.net/remita"
    }
  },
  "WebhookSecrets": {
    "stripe": "whsec_your_stripe_webhook_secret",
    "paystack": "your_paystack_webhook_secret",
    "flutterwave": "your_flutterwave_webhook_secret",
    "remita": "your_remita_webhook_secret"
  },
  "AuditLog": {
    "EncryptionKey": "your_base64_encoded_32_byte_encryption_key"
  },
  "Mailgun": {
    "Domain": "your_mailgun_domain",
    "ApiKey": "your_mailgun_api_key",
    "SenderEmail": "noreply@yourdomain.com"
  }
}
```

### Option B: Use Environment Variables (Recommended for Production)

Instead of `SecureData.json`, configure secrets as environment variables in MonsterASP.net:

**In Application Settings / Environment Variables:**
```
ConnectionStrings__Remote = Server=db42073...
Jwt__KEY = YourProductionJwtKey...
Jwt__ISSUER = FintechAPI
Jwt__AUDIENCE = FintechAPIUsers
Stripe__SecretKey = sk_live_...
PaymentProviders__Paystack__SecretKey = sk_live_...
```

---

## Step 4: Run Database Migration

**CRITICAL:** The database migration MUST be run before the app will work.

### Option A: From Your Local Machine

```bash
dotnet ef database update --project Infrastructure --startup-project Web --connection "Server=db42073.public.databaseasp.net;Database=db42073;User Id=db42073;Password=9h+KT7d?2-Xx;Encrypt=True;TrustServerCertificate=True;"
```

### Option B: Generate SQL Script and Run Manually

```bash
# Generate the SQL script
dotnet ef migrations script --project Infrastructure --startup-project Web --idempotent --output migration.sql

# Then run migration.sql in MonsterASP.net SQL Manager or SSMS
```

### Option C: Add Migration to Startup (Not Recommended for Production)

In `Program.cs`, add before `app.Run()`:

```csharp
// Auto-migrate on startup (use with caution in production)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}
```

---

## Step 5: Configure Application Pool

In MonsterASP.net control panel:

1. **Application Pool Settings:**
   - .NET CLR Version: **No Managed Code**
   - Managed Pipeline Mode: **Integrated**
   - Enable 32-Bit Applications: **False**
   - Start Mode: **AlwaysRunning** (for background services)

2. **Identity:** ApplicationPoolIdentity (default)

3. **Recycling:** Configure to minimize disruption
   - Regular Time Interval: 1740 minutes (29 hours)
   - Specific Times: Off-peak hours

---

## Step 6: Create Logs Folder

The app needs a `logs` folder for stdout logging:

1. In MonsterASP.net File Manager
2. Create folder: `logs`
3. Set permissions: Write access for application pool identity

---

## Step 7: Deploy and Test

### Trigger Deployment

1. Push changes to GitHub
2. MonsterASP.net will automatically build and deploy
3. Monitor the build log for errors

### Check Deployment Status

1. **Build Log:** Check for compilation errors
2. **Stdout Logs:** Check `logs/stdout_*.log` for runtime errors
3. **Application Log:** Check Windows Event Viewer (if accessible)

### Test the Application

1. Browse to your site: `https://yoursite.monsterasp.net`
2. Check Swagger: `https://yoursite.monsterasp.net/swagger`
3. Test an API endpoint
4. Verify background services are running (check logs)

---

## Common GitHub Deployment Issues

### Issue 1: Build Fails - Missing Dependencies

**Error:** "Could not find project or directory"

**Solution:** Ensure all `.csproj` files are in GitHub:
```bash
git add **/*.csproj
git commit -m "Add project files"
git push
```

### Issue 2: Build Succeeds but App Crashes

**Error:** 403 Forbidden or 500 Internal Server Error

**Solution:** 
1. Check `logs/stdout_*.log` for exact error
2. Most likely: Missing `SecureData.json` or database migration not run

### Issue 3: Database Connection Fails

**Error:** "A network-related or instance-specific error"

**Solution:**
1. Verify connection string in `SecureData.json`
2. Check if MonsterASP.net IP is whitelisted in database firewall
3. Test connection from MonsterASP.net server

### Issue 4: Missing DLLs

**Error:** "Could not load file or assembly"

**Solution:** Deploy as self-contained:
```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

---

## Continuous Deployment Workflow

### Recommended Git Workflow

```bash
# Development
git checkout develop
# Make changes
git add .
git commit -m "Add new feature"
git push origin develop

# When ready for production
git checkout main
git merge develop
git push origin main  # This triggers MonsterASP.net deployment
```

### Pre-Deployment Checklist

Before pushing to `main`:

- [ ] All tests pass locally
- [ ] Database migrations created (if schema changed)
- [ ] No secrets in code
- [ ] `web.config` is up to date
- [ ] Build succeeds locally: `dotnet build -c Release`
- [ ] Publish succeeds locally: `dotnet publish -c Release`

---

## Monitoring After Deployment

### Check These After Each Deployment:

1. **Application Status**
   - Site loads without errors
   - Swagger UI accessible
   - API endpoints responding

2. **Background Services**
   - Check logs for "Reconciliation Background Service started"
   - Check logs for "Secret Rotation Background Service started"

3. **Database**
   - Migrations applied successfully
   - New tables exist
   - Data integrity maintained

4. **Logs**
   - No errors in `logs/stdout_*.log`
   - No exceptions in application logs

---

## Rollback Procedure

If deployment fails:

### Option 1: Revert Git Commit

```bash
git revert HEAD
git push origin main
```

MonsterASP.net will automatically deploy the previous version.

### Option 2: Manual Rollback

1. In MonsterASP.net, select previous successful deployment
2. Click "Redeploy"

---

## Security Best Practices

### Never Commit These to GitHub:

- ❌ Database passwords
- ❌ API keys (Stripe, Paystack, etc.)
- ❌ JWT signing keys
- ❌ Webhook secrets
- ❌ Email service credentials
- ❌ Any production secrets

### Use These Instead:

- ✅ Environment variables in MonsterASP.net
- ✅ Azure Key Vault (for enterprise)
- ✅ Separate `SecureData.json` added manually
- ✅ GitHub Secrets (for CI/CD)

---

## Troubleshooting Deployment

### Get Detailed Error Information

1. **Enable stdout logging** (already in web.config)
2. **Check logs folder** after deployment
3. **Read stdout_*.log** for exact error

### Common Errors and Solutions

| Error | Cause | Solution |
|-------|-------|----------|
| 403 Forbidden | App crashed on startup | Check stdout logs |
| 500 Internal Server Error | Unhandled exception | Check stdout logs |
| Invalid object name 'AuditLogs' | Migration not run | Run database migration |
| Value cannot be null (Parameter 's') | Missing JWT key | Add to SecureData.json |
| Could not load assembly | Missing DLLs | Deploy self-contained |

---

## Post-Deployment Tasks

After successful deployment:

1. **Run Database Migration** (if not automated)
2. **Test all API endpoints**
3. **Verify background services are running**
4. **Check webhook endpoints are accessible**
5. **Monitor logs for first 24 hours**
6. **Set up monitoring/alerting**

---

## Support

If deployment fails:

1. **Check stdout logs** in `logs` folder
2. **Review build log** in MonsterASP.net
3. **Verify all steps** in this guide
4. **Contact MonsterASP.net support** with:
   - Build log
   - Stdout log
   - Error screenshots

---

## Summary

**GitHub Deployment Steps:**

1. ✅ Ensure `web.config` is in repository
2. ✅ Push code to GitHub
3. ✅ Configure MonsterASP.net GitHub integration
4. ✅ Add `SecureData.json` manually (not in Git)
5. ✅ Run database migration
6. ✅ Configure application pool
7. ✅ Create `logs` folder
8. ✅ Test deployment
9. ✅ Monitor logs

**The most important step:** Run the database migration before the app will work!

```bash
dotnet ef database update --project Infrastructure --startup-project Web
```
