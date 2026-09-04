# Quick Firewall Setup for Tapo Camera RTSP Streaming
# Run as Administrator in PowerShell

# One-liner commands for quick firewall configuration
Write-Host "🔥 Quick Firewall Setup for Tapo Camera Streaming" -ForegroundColor Green

# Essential RTSP and camera ports
New-NetFirewallRule -DisplayName "Tapo-RTSP-554" -Direction Outbound -Protocol TCP -RemotePort 554 -Action Allow -Profile Any
New-NetFirewallRule -DisplayName "Tapo-ONVIF-2020" -Direction Outbound -Protocol TCP -RemotePort 2020 -Action Allow -Profile Any  
New-NetFirewallRule -DisplayName "Tapo-HTTPS-443" -Direction Outbound -Protocol TCP -RemotePort 443 -Action Allow -Profile Any
New-NetFirewallRule -DisplayName "Tapo-Streaming-8800" -Direction Outbound -Protocol TCP -RemotePort 8800 -Action Allow -Profile Any
New-NetFirewallRule -DisplayName "Tapo-Discovery-UDP" -Direction Outbound -Protocol UDP -RemotePort 9999,20002 -Action Allow -Profile Any
New-NetFirewallRule -DisplayName "Tapo-Camera-Responses" -Direction Inbound -Protocol Any -RemoteAddress "192.168.0.0/16,10.0.0.0/8" -Action Allow -Profile Private,Domain

Write-Host "✅ Essential firewall rules created for Tapo camera streaming!" -ForegroundColor Green
Write-Host "📱 Test your TapoMaui app now - RTSP port 554 should be accessible" -ForegroundColor Cyan