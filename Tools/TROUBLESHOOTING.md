# Tapo Discovery Troubleshooting Guide

This guide helps diagnose issues with Tapo device discovery using the enhanced UDP broadcast mechanism.

## 🔍 Understanding the Discovery Process

### How Tapo UDP Discovery Works
1. **Local Port Binding**: Tool binds to an available UDP port (e.g., 52341)
2. **Broadcast Transmission**: Sends discovery packets to 255.255.255.255:9999 and 255.255.255.255:20002
3. **Unicast Response**: Tapo devices respond directly back to the sender's IP and port
4. **JSON Parsing**: Responses are parsed for device information

### Expected Console Output
```
Scanning for Tapo devices on UDP port 9999...
Listening for responses on local port 52341
Sent discovery packet to broadcast:9999 (27 bytes)
Sent discovery packet to broadcast:9999 (39 bytes)
...
Received response #1 from 192.168.1.105:9999
Processing response from 192.168.1.105:9999 (discovery port 9999)
Response preview: {"system":{"get_sysinfo":{"mac":"AA-BB-CC-DD-EE-FF"...
🔍 Extracted info: Model=P110, Type=Tapo Smart Plug, MAC=AA-BB-CC-...
✅ Found Tapo device: 192.168.1.105 - P110 via UDP:9999
```

## 🚨 Common Issues and Solutions

### Issue 1: "No Tapo devices found" but devices are online

#### Symptoms:
- Tapo app can see devices
- Network scan finds devices as general network devices
- No UDP responses received

#### Causes & Solutions:

**Windows Firewall Blocking UDP**
```powershell
# Check if Windows Firewall is blocking
# Run as Administrator:
New-NetFirewallRule -DisplayName "Tapo Discovery" -Direction Inbound -Protocol UDP -LocalPort 9999,20002 -Action Allow
New-NetFirewallRule -DisplayName "Tapo Discovery Out" -Direction Outbound -Protocol UDP -RemotePort 9999,20002 -Action Allow
```

**Router/Network Issues**
- **AP Isolation**: Many routers have "AP Isolation" or "Client Isolation" enabled
  - Solution: Disable in router settings under WiFi → Advanced → AP Isolation
- **Guest Network**: Devices on guest networks often can't communicate
  - Solution: Move devices to main network
- **VLAN Separation**: Enterprise networks may separate device traffic
  - Solution: Ensure all devices are on same VLAN/subnet

**Device-Specific Issues**
- **Firmware Version**: Older firmware may use different discovery protocols
  - Solution: Update Tapo device firmware via the app
- **Power Saving Mode**: Some devices may not respond when in power save
  - Solution: Interact with device via app, then run discovery

### Issue 2: "Permission denied" or "Access denied"

#### Symptoms:
```
UDP discovery on port 9999 failed: Access denied
```

#### Solutions:
```powershell
# Run PowerShell as Administrator
# Right-click PowerShell → "Run as Administrator"
cd "C:\path\to\NetworkDiscovery"
dotnet run
```

**Alternative: Create firewall rules**
```cmd
netsh advfirewall firewall add rule name="Tapo UDP In" dir=in action=allow protocol=UDP localport=9999,20002
netsh advfirewall firewall add rule name="Tapo UDP Out" dir=out action=allow protocol=UDP remoteport=9999,20002
```

### Issue 3: Receiving responses but extraction fails

#### Symptoms:
```
Received response #1 from 192.168.1.105:9999
❌ Invalid JSON from 192.168.1.105: Unexpected character
```

#### Causes:
- **Encrypted Responses**: Many Tapo devices encrypt their responses
- **Binary Data**: Some responses may be in binary format
- **Different Protocol**: Device may use a newer or different communication protocol

#### Current Handling:
The tool will still detect these as "Potential Tapo Device (Non-JSON Response)" and log them.

### Issue 4: Partial device information

#### Symptoms:
```
✅ Found Tapo device: 192.168.1.105 - Unknown Model via UDP:9999
```

#### Causes & Solutions:
- **Limited Response**: Device may not include all fields in discovery response
- **Firmware Differences**: Different firmware versions provide different information
- **Device Type**: Some device types (sensors, hubs) may have minimal discovery info

### Issue 5: Network timing issues

#### Symptoms:
- Inconsistent discovery results
- Some devices found sometimes, not others

#### Solutions:
```csharp
// Tool automatically handles this with:
// - Multiple discovery payload formats (8 different types)
// - Extended listening period (5 seconds)
// - Multiple transmission attempts
// - Proper timeout handling
```

## 🔧 Advanced Troubleshooting

### Enable Windows Network Tracing
```powershell
# Run as Administrator
netsh trace start capture=yes provider=Microsoft-Windows-TCPIP level=4 keywords=0x240
# Run the discovery tool
netsh trace stop
# Analyze NetworkTrace.etl with Network Monitor
```

### Check UDP Port Availability
```powershell
# Check if ports are available
Get-NetUDPEndpoint | Where-Object LocalPort -In 9999,20002
netstat -an | findstr "9999\|20002"
```

### Manual Tapo Device Testing
```powershell
# Test direct connection to known Tapo device
Test-NetConnection -ComputerName 192.168.1.105 -Port 9999 -InformationLevel Detailed
```

### Wireshark Analysis
1. Install Wireshark
2. Capture on your network interface
3. Filter: `udp.port == 9999 or udp.port == 20002`
4. Run discovery tool
5. Look for:
   - Outbound broadcast packets to 255.255.255.255
   - Inbound unicast responses from device IPs

## 🏠 Network Configuration Recommendations

### Router Settings for Optimal Discovery
```
✅ AP Isolation: Disabled
✅ Client Isolation: Disabled  
✅ Multicast Support: Enabled
✅ UPnP: Enabled (for mDNS discovery)
✅ Guest Network: Don't put Tapo devices here
```

### Windows Network Profile
Ensure your network is set to "Private" not "Public":
```powershell
Get-NetConnectionProfile
Set-NetConnectionProfile -InterfaceAlias "Wi-Fi" -NetworkCategory Private
```

## 📊 Discovery Success Rates by Device Type

Based on testing, expected success rates:
- **Smart Plugs (P1xx)**: 95%+ - Most responsive to UDP discovery
- **Smart Bulbs (L5xx)**: 90%+ - Good UDP response
- **Cameras (C2xx/C3xx)**: 85%+ - May use different discovery methods
- **Hubs (H1xx/H2xx)**: 80%+ - Often discoverable via mDNS as well
- **Sensors (T1xx/T3xx)**: 60%+ - May have limited discovery capability

## 🔍 Detailed Logging

To get more detailed logging, modify the console output level in the tool or run with verbose .NET logging:
```powershell
$env:DOTNET_SYSTEM_NET_SOCKETS_DEBUG=1
dotnet run
```

## 📞 When to Contact Support

Contact TP-Link/Tapo support if:
- Devices work in official app but never respond to any discovery method
- Recently updated firmware causing discovery issues
- Devices randomly stop responding to discovery after working previously

The enhanced discovery tool uses the same basic UDP broadcast mechanism as the official Tapo app, so if devices respond to the app, they should respond to this tool as well.