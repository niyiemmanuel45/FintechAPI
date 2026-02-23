# GitHub to MonsterASP.net - Quick Deployment Checklist

## ✅ Pre-Deployment (Do This First!)

### 1. Verify Files in Repository
```bash
# Check that web.config exists
git ls-files | grep web.config

# Check that SecureData.json is NOT in repository (should return nothing)
git ls-files | grep SecureData.json

# If SecureData.json is in Git, remove it:
git rm --cached Web/SecureData.json
git commit -m "Remove SecureData.json from repository"
git push
```

### 2. Commit Required Files
```bash
git add Web/web.config
git add .gitignore
git add GITHUB_DEPLOYMENT_GUIDE.md
git add MONSTERASP_QUICK_FIX.md
git commit -m "Add deployment configuration"
git push origin main
```

---

## ✅ MonsterASP.net Configuration

### 1. GitHub Integration Setup
- [ ] Connect GitHub repository
- [ ] Select branch: `main`
- [ ] Set project path: `Web/Web.csproj`
- [ ] Build configuration: `Release`
- [ ] Runtime: `win-x64` (self-contained)

### 2. Application Pool Settings
- [ ] .NET CLR Version: **No Managed Code**
- [ ] Managed Pipeline Mode: **Integrated**
- [ ] Enable 32-Bit: **False**
- [ ] Start Mode: **AlwaysRunning**

### 3. Create Logs Folder
- [ ] Create `logs` folder in site root
- [ ] Set write permissions for application pool

---

## ✅ Add Secrets (CRITICAL!)

### Option A: Manual File Upload

Upload `SecureData.json` to site root with:

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
  }
}
```

### Option B: Environment Variables (Recommended)

Set in MonsterASP.net Application Settings:
- `ConnectionStrings__Remote`
- `Jwt__KEY`
- `Jwt__ISSUER`
- `Jwt__AUDIENCE`
- `Stripe__SecretKey`

---

## ✅ Database Migration (MUST DO!)

### Run from Local Machine:
```bash
dotnet ef database update --project Infrastructure --startup-project Web --connection "Server=db42073.public.databaseasp.net;Database=db42073;User Id=db42073;Password=9h+KT7d?2-Xx;Encrypt=True;TrustServerCertificate=True;"
```

### Or Generate SQL Script:
```bash
dotnet ef migrations script --project Infrastructure --startup-project Web --idempotent --output migration.sql
```

Then run `migration.sql` in MonsterASP.net SQL Manager.

---

## ✅ Deploy and Test

### 1. Trigger Deployment
```bash
git push origin main
```

### 2. Monitor Build
- [ ] Check MonsterASP.net build log
- [ ] Wait for "Deployment successful" message

### 3. Check Logs
- [ ] Browse to site
- [ ] Check `logs/stdout_*.log` for errors
- [ ] Look for "Application started" message

### 4. Test Application
- [ ] Site loads: `https://yoursite.monsterasp.net`
- [ ] Swagger works: `https://yoursite.monsterasp.net/swagger`
- [ ] Test API endpoint
- [ ] Verify background services started (check logs)

---

## ✅ Troubleshooting

### If Site Shows 403 Error:

1. **Check stdout logs** in `logs` folder
2. **Most common causes:**
   - Database migration not run → Run migration
   - SecureData.json missing → Upload it
   - .NET 8.0 not installed → Deploy self-contained

### If Build Fails:

1. **Check build log** in MonsterASP.net
2. **Common issues:**
   - Missing .csproj files → Commit them to Git
   - Compilation errors → Fix and push again
   - Missing dependencies → Check NuGet packages

### Get Help:

Share these with support:
- Build log from MonsterASP.net
- Contents of `logs/stdout_*.log`
- Screenshot of error

---

## ✅ Post-Deployment Verification

### Check These:

- [ ] Site loads without errors
- [ ] Swagger UI accessible
- [ ] Database tables exist (AuditLogs, ReconciliationReports, etc.)
- [ ] Background services running (check logs)
- [ ] No errors in stdout logs
- [ ] API endpoints responding correctly

### Monitor for 24 Hours:

- [ ] Check logs daily
- [ ] Monitor application pool recycling
- [ ] Verify background services run on schedule
- [ ] Test payment provider integrations

---

## 🚨 CRITICAL REMINDERS

1. **NEVER commit SecureData.json to GitHub!**
2. **ALWAYS run database migration before first deployment**
3. **CHECK stdout logs if anything fails**
4. **Keep production secrets separate from code**

---

## Quick Commands Reference

```bash
# Check what's in Git
git ls-files

# Remove secret file from Git
git rm --cached Web/SecureData.json

# Push to trigger deployment
git push origin main

# Run database migration
dotnet ef database update --project Infrastructure --startup-project Web

# Generate migration SQL
dotnet ef migrations script --project Infrastructure --startup-project Web --output migration.sql

# Test build locally
dotnet build -c Release

# Test publish locally
dotnet publish Web/Web.csproj -c Release -r win-x64 --self-contained true
```

---

## Success Criteria

✅ Deployment is successful when:
- Build completes without errors
- Site loads without 403/500 errors
- Swagger UI is accessible
- API endpoints respond correctly
- Background services are running
- No errors in stdout logs

---

## Need Help?

1. Read `GITHUB_DEPLOYMENT_GUIDE.md` for detailed instructions
2. Read `MONSTERASP_QUICK_FIX.md` for troubleshooting
3. Check stdout logs in `logs` folder
4. Contact MonsterASP.net support with logs

**The stdout logs will tell you exactly what's wrong!**
