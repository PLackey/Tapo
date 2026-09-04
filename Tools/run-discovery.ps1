Write-Host "Starting Network Discovery Tool..." -ForegroundColor Green
Write-Host ""
Set-Location "NetworkDiscovery"
dotnet run
Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")