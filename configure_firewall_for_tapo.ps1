# Configure Windows Firewall for TapoMaui and VLC RTSP Access
# Run as Administrator

Write-Host "🔥 Configuring Windows Firewall for Tapo Camera RTSP Streaming..." -ForegroundColor Green
Write-Host ""

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "❌ This script requires Administrator privileges!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Running with Administrator privileges" -ForegroundColor Green
Write-Host ""

# === OUTBOUND RULES FOR RTSP STREAMING ===
Write-Host "📡 Creating outbound firewall rules for RTSP streaming..." -ForegroundColor Cyan

# RTSP Port 554 (Primary streaming port)
try {
    New-NetFirewallRule -DisplayName "TapoMaui - RTSP Outbound (554)" `
                        -Direction Outbound `
                        -Protocol TCP `
                        -RemotePort 554 `
                        -Action Allow `
                        -Profile Any `
                        -Description "Allow TapoMaui to connect to Tapo cameras via RTSP port 554"
    Write-Host "✅ RTSP Port 554 (TCP Outbound) - Rule created" -ForegroundColor Green
} catch {
    Write-Host "⚠️ RTSP Port 554 rule may already exist: $($_.Exception.Message)" -ForegroundColor Yellow
}

# ONVIF Port 2020 (Camera management)
try {
    New-NetFirewallRule -DisplayName "TapoMaui - ONVIF Outbound (2020)" `
                        -Direction Outbound `
                        -Protocol TCP `
                        -RemotePort 2020 `
                        -Action Allow `
                        -Profile Any `
                        -Description "Allow TapoMaui to connect to Tapo cameras via ONVIF port 2020"
    Write-Host "✅ ONVIF Port 2020 (TCP Outbound) - Rule created" -ForegroundColor Green
} catch {
    Write-Host "⚠️ ONVIF Port 2020 rule may already exist: $($_.Exception.Message)" -ForegroundColor Yellow
}

# TP-Link Streaming Port 8800
try {
    New-NetFirewallRule -DisplayName "TapoMaui - TP-Link Streaming (8800)" `
                        -Direction Outbound `
                        -Protocol TCP `
                        -RemotePort 8800 `
                        -Action Allow `
                        -Profile Any `
                        -Description "Allow TapoMaui to connect to TP-Link proprietary streaming port 8800"
    Write-Host "✅ TP-Link Port 8800 (TCP Outbound) - Rule created" -ForegroundColor Green
} catch {
    Write-Host "⚠️ TP-Link Port 8800 rule may already exist: $($_.Exception.Message)" -ForegroundColor Yellow
}

# HTTPS Port 443 (Camera web interface)
try {
    New-NetFirewallRule -DisplayName "TapoMaui - HTTPS Outbound (443)" `
                        -Direction Outbound `
                        -Protocol TCP `
                        -RemotePort 443 `
                        -Action Allow `
                        -Profile Any `
                        -Description "Allow TapoMaui HTTPS access to Tapo cameras"
    Write-Host "✅ HTTPS Port 443 (TCP Outbound) - Rule created" -ForegroundColor Green
} catch {
    Write-Host "⚠️ HTTPS Port 443 rule may already exist: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# === APPLICATION-SPECIFIC RULES ===
Write-Host "🎯 Creating application-specific firewall rules..." -ForegroundColor Cyan

# TapoMaui Application Rule
$tapoMauiPath = "C:\Users\$env:USERNAME\OneDrive\Documents\dev\tapo\tapo\TapoMaui\bin\Debug\net10.0-windows10.0.19041.0\win-x64\TapoMaui.exe"
if (Test-Path $tapoMauiPath) {
    try {
        New-NetFirewallRule -DisplayName "TapoMaui Application - All Traffic" `
                            -Direction Outbound `
                            -Program $tapoMauiPath `
                            -Action Allow `
                            -Profile Any `
                            -Description "Allow TapoMaui application full outbound network access"
        Write-Host "✅ TapoMaui Application Rule - Created for: $tapoMauiPath" -ForegroundColor Green
    } catch {
        Write-Host "⚠️ TapoMaui application rule may already exist: $($_.Exception.Message)" -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠️ TapoMaui.exe not found at expected path. Build the app first, then re-run this script." -ForegroundColor Yellow
    Write-Host "   Expected: $tapoMauiPath" -ForegroundColor Gray
}

# VLC Media Player Rule
$vlcPaths = @(
    "${env:ProgramFiles}\VideoLAN\VLC\vlc.exe",
    "${env:ProgramFiles(x86)}\VideoLAN\VLC\vlc.exe",
    "$env:LOCALAPPDATA\Programs\VLC\vlc.exe"
)

$vlcFound = $false
foreach ($vlcPath in $vlcPaths) {
    if (Test-Path $vlcPath) {
        try {
            New-NetFirewallRule -DisplayName "VLC Media Player - RTSP Streaming" `
                                -Direction Outbound `
                                -Program $vlcPath `
                                -Action Allow `
                                -Profile Any `
                                -Description "Allow VLC Media Player to access RTSP streams from Tapo cameras"
            Write-Host "✅ VLC Media Player Rule - Created for: $vlcPath" -ForegroundColor Green
            $vlcFound = $true
            break
        } catch {
            Write-Host "⚠️ VLC rule may already exist: $($_.Exception.Message)" -ForegroundColor Yellow
            $vlcFound = $true
            break
        }
    }
}

if (-not $vlcFound) {
    Write-Host "⚠️ VLC Media Player not found. Install VLC, then re-run this script." -ForegroundColor Yellow
    Write-Host "   Download from: https://www.videolan.org/vlc/" -ForegroundColor Gray
}

Write-Host ""

# === UDP DISCOVERY PORTS ===
Write-Host "🔍 Creating rules for Tapo device discovery..." -ForegroundColor Cyan

# UDP Discovery Port 9999 (Tapo plugs/bulbs)
try {
    New-NetFirewallRule -DisplayName "TapoMaui - Discovery UDP (9999)" `
                        -Direction Outbound `
                        -Protocol UDP `
                        -RemotePort 9999 `
                        -Action Allow `
                        -Profile Any `
                        -Description "Allow TapoMaui UDP discovery for Tapo smart plugs and bulbs"
    Write-Host "✅ UDP Discovery Port 9999 - Rule created" -ForegroundColor Green
} catch {
    Write-Host "⚠️ UDP 9999 rule may already exist: $($_.Exception.Message)" -ForegroundColor Yellow
}

# UDP Discovery Port 20002 (Tapo cameras)
try {
    New-NetFirewallRule -DisplayName "TapoMaui - Camera Discovery UDP (20002)" `
                        -Direction Outbound `
                        -Protocol UDP `
                        -RemotePort 20002 `
                        -Action Allow `
                        -Profile Any `
                        -Description "Allow TapoMaui UDP discovery for Tapo cameras"
    Write-Host "✅ UDP Discovery Port 20002 - Rule created" -ForegroundColor Green
} catch {
    Write-Host "⚠️ UDP 20002 rule may already exist: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# === INBOUND RULES FOR RESPONSES ===
Write-Host "📥 Creating inbound rules for camera responses..." -ForegroundColor Cyan

# Allow inbound responses from camera subnet (adjust IP range as needed)
try {
    New-NetFirewallRule -DisplayName "TapoMaui - Camera Responses (192.168.x.x)" `
                        -Direction Inbound `
                        -Protocol Any `
                        -RemoteAddress "192.168.0.0/16" `
                        -Action Allow `
                        -Profile Private,Domain `
                        -Description "Allow inbound responses from Tapo cameras on local network"
    Write-Host "✅ Inbound Camera Responses (192.168.x.x) - Rule created" -ForegroundColor Green
} catch {
    Write-Host "⚠️ Inbound camera response rule may already exist: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Allow inbound responses from 10.x.x.x subnet
try {
    New-NetFirewallRule -DisplayName "TapoMaui - Camera Responses (10.x.x.x)" `
                        -Direction Inbound `
                        -Protocol Any `
                        -RemoteAddress "10.0.0.0/8" `
                        -Action Allow `
                        -Profile Private,Domain `
                        -Description "Allow inbound responses from Tapo cameras on 10.x.x.x networks"
    Write-Host "✅ Inbound Camera Responses (10.x.x.x) - Rule created" -ForegroundColor Green
} catch {
    Write-Host "⚠️ Inbound 10.x.x.x response rule may already exist: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🎯 FIREWALL CONFIGURATION SUMMARY:" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host "✅ RTSP Port 554 (TCP) - Camera streaming" -ForegroundColor White
Write-Host "✅ ONVIF Port 2020 (TCP) - Camera management" -ForegroundColor White
Write-Host "✅ TP-Link Port 8800 (TCP) - Proprietary streaming" -ForegroundColor White
Write-Host "✅ HTTPS Port 443 (TCP) - Camera web interface" -ForegroundColor White
Write-Host "✅ UDP Ports 9999, 20002 - Device discovery" -ForegroundColor White
Write-Host "✅ Application rules for TapoMaui and VLC" -ForegroundColor White
Write-Host "✅ Inbound rules for camera responses" -ForegroundColor White
Write-Host ""
Write-Host "📱 NEXT STEPS:" -ForegroundColor Cyan
Write-Host "1. Test TapoMaui camera discovery and streaming" -ForegroundColor White
Write-Host "2. If still blocked, check your router's firewall settings" -ForegroundColor White
Write-Host "3. Verify camera RTSP is enabled in Tapo mobile app" -ForegroundColor White
Write-Host "4. Use 'Test' button in TapoMaui for connection diagnosis" -ForegroundColor White
Write-Host ""
Write-Host "🔧 To view created rules: Windows Security → Firewall → Advanced settings" -ForegroundColor Yellow
Write-Host "🗑️ To remove rules later: Get-NetFirewallRule -DisplayName '*TapoMaui*' | Remove-NetFirewallRule" -ForegroundColor Yellow
Write-Host ""
Write-Host "✅ Firewall configuration complete!" -ForegroundColor Green