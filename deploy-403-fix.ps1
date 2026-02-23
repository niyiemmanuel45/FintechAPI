# Deploy 403 Fix to Production
# This script commits and pushes all the fixes for the 403 Forbidden error

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deploy 403 Fix to Production" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Files to be committed:" -ForegroundColor Yellow
Write-Host "  - Application/Services/ReconciliationBackgroundService.cs (resilient startup)" -ForegroundColor Green
Write-Host "  - Application/Services/SecretRotationBackgroundService.cs (resilient startup)" -ForegroundColor Green
Write-Host "  - Web/Controllers/HomeController.cs (fixed redirect)" -ForegroundColor Green
Write-Host "  - CRITICAL_403_FIX.md (documentation)" -ForegroundColor Green
Write-Host "  - run-production-migration.ps1 (migration script)" -ForegroundColor Green
Write-Host ""

$confirm = Read-Host "Do you want to commit and push these changes? (yes/no)"

if ($confirm -ne "yes") {
    Write-Host "Deployment cancelled." -ForegroundColor Red
    exit
}

Write-Host ""
Write-Host "Adding files to git..." -ForegroundColor Cyan

git add Application/Services/ReconciliationBackgroundService.cs
git add Application/Services/SecretRotationBackgroundService.cs
git add Web/Controllers/HomeController.cs
git add CRITICAL_403_FIX.md
git add run-production-migration.ps1
git add deploy-403-fix.ps1

Write-Host "Committing changes..." -ForegroundColor Cyan
git commit -m "Fix: Resolve 403 error - Make background services resilient and fix HomeController redirect"

Write-Host "Pushing to GitHub..." -ForegroundColor Cyan
git push origin main

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Deployment successful!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Wait 2-3 minutes for MonsterASP.net to deploy from GitHub" -ForegroundColor White
    Write-Host "2. Visit http://fintechapi.runasp.net/ to verify it works" -ForegroundColor White
    Write-Host "3. Check that you're redirected to Swagger UI" -ForegroundColor White
    Write-Host ""
    Write-Host "If you still get 403 error:" -ForegroundColor Yellow
    Write-Host "- The database migration may not have been run" -ForegroundColor White
    Write-Host "- Run: .\run-production-migration.ps1" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "Push failed! Check the error messages above." -ForegroundColor Red
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
