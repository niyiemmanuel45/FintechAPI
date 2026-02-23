# Run Database Migration on Production
# This script applies the PaymentOrchestrationComplete migration to the production database

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Production Database Migration Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$connectionString = "Server=db42073.public.databaseasp.net;Database=db42073;User Id=db42073;Password=9h+KT7d?2-Xx;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"

Write-Host "Target Database: db42073.public.databaseasp.net" -ForegroundColor Yellow
Write-Host "Database Name: db42073" -ForegroundColor Yellow
Write-Host ""
Write-Host "This will create the following tables:" -ForegroundColor Green
Write-Host "  - AuditLogs" -ForegroundColor Green
Write-Host "  - ReconciliationReports" -ForegroundColor Green
Write-Host "  - SecretRotationHistory" -ForegroundColor Green
Write-Host "  - SecurityAuditLogs" -ForegroundColor Green
Write-Host "  - TransactionMismatches" -ForegroundColor Green
Write-Host ""

$confirm = Read-Host "Do you want to proceed? (yes/no)"

if ($confirm -ne "yes") {
    Write-Host "Migration cancelled." -ForegroundColor Red
    exit
}

Write-Host ""
Write-Host "Running migration..." -ForegroundColor Cyan

try {
    dotnet ef database update --project Infrastructure --startup-project Web --connection $connectionString
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "Migration completed successfully!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "1. Deploy the updated code to GitHub" -ForegroundColor White
        Write-Host "2. Wait for MonsterASP.net to deploy" -ForegroundColor White
        Write-Host "3. Test the application at http://fintechapi.runasp.net/" -ForegroundColor White
    } else {
        Write-Host ""
        Write-Host "Migration failed! Check the error messages above." -ForegroundColor Red
    }
} catch {
    Write-Host ""
    Write-Host "Error running migration: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
