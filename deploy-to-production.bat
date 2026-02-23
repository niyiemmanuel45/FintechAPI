@echo off
echo ========================================
echo FintechAPI Production Deployment Script
echo ========================================
echo.

REM Clean previous publish
echo [1/5] Cleaning previous publish folder...
if exist publish rmdir /s /q publish
mkdir publish
echo Done.
echo.

REM Publish the application
echo [2/5] Publishing application (Self-Contained for Windows x64)...
dotnet publish Web/Web.csproj -c Release -r win-x64 --self-contained true -o ./publish
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Publish failed!
    pause
    exit /b 1
)
echo Done.
echo.

REM Copy SecureData.json
echo [3/5] Copying SecureData.json...
copy Web\SecureData.json publish\SecureData.json
if %ERRORLEVEL% NEQ 0 (
    echo WARNING: SecureData.json not found! Make sure to add it manually.
)
echo Done.
echo.

REM Create logs folder
echo [4/5] Creating logs folder...
mkdir publish\logs 2>nul
echo Done.
echo.

REM Display summary
echo [5/5] Deployment package ready!
echo.
echo ========================================
echo DEPLOYMENT SUMMARY
echo ========================================
echo Location: .\publish\
echo.
echo FILES TO DEPLOY:
echo - All files in .\publish\ folder
echo - Make sure SecureData.json has production settings
echo.
echo BEFORE DEPLOYING:
echo 1. Update SecureData.json with production connection string
echo 2. Update JWT keys and secrets
echo 3. Run database migrations on production database
echo.
echo DATABASE MIGRATION COMMAND:
echo dotnet ef database update --project Infrastructure --startup-project Web
echo.
echo NEXT STEPS:
echo 1. Upload all files from .\publish\ to your MonsterASP.net site
echo 2. Ensure 'logs' folder has write permissions
echo 3. Configure IIS Application Pool (No Managed Code)
echo 4. Check logs\stdout*.log for any errors
echo.
echo ========================================
pause
