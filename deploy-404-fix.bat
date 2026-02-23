@echo off
echo ========================================
echo Deploying 404 Fix
echo ========================================
echo.

echo [1/3] Adding changed files...
git add Web/Controllers/HomeController.cs
git add Web/web.config
git add Web/Program.cs
git add QUICK_FIX_404.md
git add deploy-404-fix.bat
echo Done.
echo.

echo [2/3] Committing changes...
git commit -m "Fix 404 error - redirect root to Swagger and improve routing"
if %ERRORLEVEL% NEQ 0 (
    echo No changes to commit or commit failed.
    pause
    exit /b 1
)
echo Done.
echo.

echo [3/3] Pushing to GitHub...
git push origin main
if %ERRORLEVEL% NEQ 0 (
    echo Push failed! Check your Git configuration.
    pause
    exit /b 1
)
echo Done.
echo.

echo ========================================
echo DEPLOYMENT TRIGGERED!
echo ========================================
echo.
echo The changes have been pushed to GitHub.
echo MonsterASP.net/RunASP.net will automatically rebuild and deploy.
echo.
echo WAIT 2-5 MINUTES for deployment to complete.
echo.
echo Then test these URLs:
echo 1. http://fintechapi.runasp.net/ (should redirect to Swagger)
echo 2. http://fintechapi.runasp.net/swagger (should show API docs)
echo.
echo If still getting 404:
echo - Wait a bit longer for deployment
echo - Check build log in hosting panel
echo - Check logs/stdout_*.log on server
echo.
echo ========================================
pause
