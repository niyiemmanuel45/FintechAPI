# Deployment Troubleshooting Guide - MonsterASP.net

## Issue: 403 Forbidden & Process Crash (Exit Code 0xffffffff)

The application is crashing on startup on MonsterASP.net. Here are the most common causes and solutions:

---

## 1. Missing Configuration Files

### Problem
The `SecureData.json` file is likely not deployed or has incorrect permissions.

### Solution
Ensure these files are deployed:
- `appsettings.json`
- `appsettings.Production.json` (if exists)
- `SecureData.json`
- `web.config`

**Check web.config exists:**
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
                  hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

---

## 2. Database Connection Issues

### Problem
The connection string in `SecureData.json` might not be accessible from the server.

### Solution
Verify the connection string is correct for production:

```json
{
  "ConnectionStrings": {
    "Remote": "Server=db42073.public.databaseasp.net;Database=db42073;User Id=db42073;Password=9h+KT7d?2-Xx;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  }
}
```

**Test the connection:**
- Ensure the database server allows connections from MonsterASP.net IP
- Check if firewall rules are configured
- Verify SQL Server is running

---

## 3. Missing Dependencies

### Problem
Required DLLs or runtime components are missing.

### Solution

**Publish the application properly:**
```bash
dotnet publish Web/Web.csproj -c Release -o ./publish
```

This creates a self-contained deployment with all dependencies.

**Files that MUST be in the deployment:**
- Web.dll
- Application.dll
- Infrastructure.dll
- Core.dll
- Adapters.dll
- All NuGet package DLLs
- appsettings.json
- SecureData.json
- web.config

---

## 4. .NET Runtime Not Installed

### Problem
MonsterASP.net might not have .NET 8.0 runtime installed.

### Solution

**Option A: Self-Contained Deployment**
```bash
dotnet publish Web/Web.csproj -c Release -r win-x64 --self-contained true -o ./publish
```

This includes the .NET runtime in your deployment.

**Option B: Contact MonsterASP.net Support**
Ask them to install .NET 8.0 Runtime on the server.

---

## 5. Application Pool Configuration

### Problem
The application pool might be configured incorrectly.

### Solution

**Required Application Pool Settings:**
- .NET CLR Version: No Managed Code
- Managed Pipeline Mode: Integrated
- Identity: ApplicationPoolIdentity (or custom account with proper permissions)
- Enable 32-Bit Applications: False
- Start Mode: AlwaysRunning (optional, for background services)

---

## 6. File Permissions

### Problem
The application pool identity doesn't have permissions to read files or write logs.

### Solution

**Grant permissions to:**
- Application folder (Read & Execute)
- `logs` folder (Write) - for stdout logging
- `SecureData.json` (Read)

**Create logs folder if it doesn't exist:**
```
mkdir logs
```

---

## 7. Startup Errors - Enable Detailed Logging

### Problem
Can't see what's causing the crash.

### Solution

**Update web.config to enable stdout logging:**
```xml
<aspNetCore processPath="dotnet" 
            arguments=".\Web.dll" 
            stdoutLogEnabled="true" 
            stdoutLogFile=".\logs\stdout" 
            hostingModel="inprocess">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
  </environmentVariables>
</aspNetCore>
```

**Check the logs folder for stdout logs:**
- Look for files like `stdout_20260223.log`
- These will show the actual error causing the crash

---

## 8. Background Services Causing Crash

### Problem
The background services (Reconciliation, Secret Rotation) might be crashing on startup.

### Solution

**Temporarily disable background services to test:**

In `Program.cs`, comment out these lines:
```csharp
// builder.Services.AddHostedService<Application.Services.ReconciliationBackgroundService>();
// builder.Services.AddHostedService<Application.Services.SecretRotationBackgroundService>();
```

If the app starts successfully, the issue is with the background services.

---

## 9. Database Migration Not Run

### Problem
The new tables don't exist in the production database.

### Solution

**Run migrations on production database:**

**Option A: From your local machine (if you have access):**
```bash
dotnet ef database update --project Infrastructure --startup-project Web --connection "Server=db42073.public.databaseasp.net;Database=db42073;User Id=db42073;Password=9h+KT7d?2-Xx;Encrypt=True;TrustServerCertificate=True;"
```

**Option B: Generate SQL script and run manually:**
```bash
dotnet ef migrations script --project Infrastructure --startup-project Web --output migration.sql
```

Then run the SQL script on the production database using SQL Server Management Studio or the MonsterASP.net control panel.

---

## 10. Swagger Configuration Issue

### Problem
Swagger might be trying to load in production without proper configuration.

### Solution

**Disable Swagger in production (in Program.cs):**
```csharp
// Only enable Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = "swagger";
    });
}
```

---

## Quick Deployment Checklist

Use this checklist before deploying:

- [ ] Run `dotnet publish` with proper configuration
- [ ] Verify all DLLs are in publish folder
- [ ] Include `SecureData.json` with production connection string
- [ ] Include `web.config` with correct settings
- [ ] Create `logs` folder in deployment
- [ ] Run database migrations on production database
- [ ] Test connection string from production server
- [ ] Verify .NET 8.0 runtime is installed (or use self-contained)
- [ ] Check application pool settings
- [ ] Grant file permissions to application pool identity
- [ ] Enable stdout logging in web.config
- [ ] Check stdout logs for actual error

---

## Recommended Deployment Steps

1. **Build for production:**
   ```bash
   dotnet publish Web/Web.csproj -c Release -r win-x64 --self-contained true -o ./publish
   ```

2. **Copy these files to server:**
   - All files from `./publish` folder
   - `SecureData.json` (with production settings)
   - Create empty `logs` folder

3. **Run database migration:**
   ```bash
   dotnet ef database update --project Infrastructure --startup-project Web
   ```

4. **Configure IIS:**
   - Create/update application pool
   - Set .NET CLR Version to "No Managed Code"
   - Point website to publish folder

5. **Test:**
   - Browse to website
   - Check `logs` folder for errors
   - Verify database connection

---

## Getting Detailed Error Information

**To see the actual error:**

1. Enable stdout logging in web.config (see above)
2. Deploy and try to access the site
3. Check the `logs` folder for stdout log files
4. The log will show the exact exception causing the crash

**Common errors you might see:**
- `SqlException`: Database connection failed
- `FileNotFoundException`: Missing DLL or configuration file
- `InvalidOperationException`: Configuration error (missing JWT key, etc.)
- `UnauthorizedAccessException`: Permission issue

---

## Contact MonsterASP.net Support

If none of these solutions work, contact MonsterASP.net support with:

1. The stdout log file contents
2. Your application pool configuration
3. .NET runtime version installed on server
4. Any error messages from Event Viewer

---

## Production Configuration Checklist

Ensure your production configuration is correct:

**SecureData.json (Production):**
```json
{
  "ConnectionStrings": {
    "Remote": "YOUR_PRODUCTION_CONNECTION_STRING"
  },
  "Jwt": {
    "KEY": "YOUR_PRODUCTION_JWT_KEY_AT_LEAST_32_CHARS",
    "ISSUER": "FintechAPI",
    "AUDIENCE": "FintechAPIUsers"
  },
  "Stripe": {
    "SecretKey": "YOUR_PRODUCTION_STRIPE_KEY"
  },
  "PaymentProviders": {
    "Paystack": {
      "SecretKey": "YOUR_PRODUCTION_PAYSTACK_KEY",
      "WebhookSecret": "YOUR_PRODUCTION_WEBHOOK_SECRET"
    }
  }
}
```

**Important:** Never commit `SecureData.json` to source control!

---

## Next Steps

1. Enable stdout logging
2. Deploy and check logs
3. Share the log contents if you need more help
4. Verify database migrations are applied
5. Test each component individually

The stdout logs will tell us exactly what's wrong!
