# Enhanced Network Discovery Tool with Tapo Detection

This repository contains a comprehensive .NET-based network discovery tool that can scan your local network and identify connected devices, with specialized detection capabilities for TP-Link Tapo smart devices.

## 🎯 Key Features

### 🔌 **Tapo Smart Device Discovery**
- **UDP Broadcast Discovery** on ports 9999 and 20002
- **mDNS Service Discovery** for HomeKit and Matter-compatible devices
- **Detailed Device Information** including model, MAC address, firmware versions
- **Device Classification** for plugs, bulbs, cameras, and switches

### 🌐 **General Network Discovery**
- **IP Range Scanning** for all network devices
- **Port-based Device Identification** 
- **Hostname Resolution** and response time measurement
- **Concurrent Scanning** for fast discovery

## Project Structure

```
network discovery/
├── NetworkDiscovery/           # Main C# console application
│   ├── Program.cs             # Enhanced application with Tapo discovery
│   ├── NetworkDiscovery.csproj # Project file
│   └── README.md              # Detailed application documentation
├── run-discovery.bat          # Windows batch file to run the tool
├── run-discovery.ps1          # PowerShell script to run the tool
└── README.md                  # This file
```

## Quick Start

### Option 1: Using Batch File (Windows Command Prompt)
```cmd
run-discovery.bat
```

### Option 2: Using PowerShell Script
```powershell
.\run-discovery.ps1
```

### Option 3: Manual Execution
```cmd
cd NetworkDiscovery
dotnet run
```

## What the Tool Discovers

### 🔌 Tapo Devices (Specialized Detection)
The tool uses two primary methods to discover Tapo devices:

#### 1. UDP Broadcast Discovery
- **Port 9999**: Smart plugs (P100, P110, P115), bulbs (L510, L530), switches
- **Port 20002**: Cameras (C200, C210, C220, C310), doorbells, security devices
- **JSON Response Parsing**: Extracts device model, MAC address, versions, aliases

#### 2. mDNS Discovery  
- **HomeKit Services** (`_hap._tcp.local`): Tapo devices with HomeKit support
- **Matter Services** (`_matterc._udp.local`): Newer Matter-compatible devices
- **TP-Link Services** (`_tplink._tcp.local`): Manufacturer-specific services

### 🌐 General Network Devices
1. **Detects Your Network Interfaces**: Automatically finds your active Ethernet and Wi-Fi connections
2. **Scans IP Ranges**: Pings all addresses in your network subnet (typically 192.168.x.x or 10.x.x.x)
3. **Identifies Devices**: For each responding device, it:
   - Resolves the hostname when possible
   - Scans common ports to identify services
   - Attempts to categorize the device type
   - Measures response time

## Sample Output

```
Enhanced Network Discovery Tool with Tapo Detection
==================================================

Active Network Interfaces:
  192.168.1.100

Starting Tapo device discovery...
Scanning for Tapo devices on UDP port 9999...
Scanning for Tapo devices on UDP port 20002...
Scanning for Tapo devices via mDNS...
Starting general network scan...
Scanning network: 192.168.1/24
Found Tapo device: 192.168.1.105 - P110 via UDP:9999
Found: 192.168.1.1 (router.home) - 2ms

Network Discovery Results:
==========================

🔌 TAPO DEVICES FOUND (1):
═══════════════════════════════
📍 IP Address: 192.168.1.105
   Device Type: SMART.TAPOPLUG
   Model: P110
   Discovery Method: UDP Broadcast (Port 9999)
   MAC Address: AA:BB:CC:DD:EE:FF
   Alias: Living Room Lamp
   Hardware Version: 1.0
   Software Version: 1.2.3

🌐 OTHER NETWORK DEVICES (1):
═══════════════════════════════════════
📍 IP Address: 192.168.1.1
   Hostname: router.home
   Device Type: Web Server
   Response Time: 2ms
   Open Ports: 53, 80, 443

═══════════════════════════════════════
📊 DISCOVERY SUMMARY:
   • Tapo Devices: 1
   • Other Devices: 1
   • Total Devices: 2
```

## Device Types Detected

### Tapo Smart Devices
- **Smart Plugs**: P100, P110 (with energy monitoring), P115, P125
- **Smart Bulbs**: L510E, L530E, L900 series (color), L920 (light strip)
- **Smart Switches**: S200B (button), S220 (dimmer switch)
- **Security Cameras**: C200, C210, C220, C310, C320WS, C420, C520WS
- **Video Doorbells**: D230S1, D130
- **Smart Hubs**: H100, H200 (with alarm system)
- **Sensors**: T100, T110, T300, T315 (motion, contact, temperature)

### Other Network Devices
- **Routers/Gateways**: Web servers with DNS (ports 80, 443, 53)
- **Windows Computers**: RPC and file sharing services (ports 135, 139, 445)
- **Linux/Unix Systems**: SSH servers (port 22)
- **Network Printers**: Print services (ports 631, 9100)
- **Apple Devices**: AFP or Bonjour services (ports 548, 5353)
- **IoT Devices**: UPnP services (port 1900)

## Requirements

- **Operating System**: Windows (optimized for Windows, but cross-platform compatible)
- **.NET Runtime**: .NET 11.0 or later
- **Network Access**: Tool needs to send ping, TCP, UDP broadcast, and multicast requests
- **Permissions**: May require administrator privileges for broadcast operations

## Security Considerations

- ⚠️ **Use Responsibly**: Only scan networks you own or have explicit permission to scan
- 🛡️ **Network Security**: Some security systems may detect and log scanning activity
- 🔒 **Broadcast Traffic**: Uses UDP broadcast which may be monitored by network security systems
- 📊 **Non-Intrusive**: Doesn't attempt to exploit or access devices, only identifies them
- 🔐 **Encryption Aware**: Handles both encrypted and unencrypted Tapo device responses

## Performance

- **Tapo Discovery**: 3-8 seconds for UDP broadcast + mDNS discovery
- **Network Scan**: Typically scans a full /24 subnet (254 addresses) in 10-30 seconds
- **Concurrent Operations**: Tapo discovery runs in parallel with general network scanning
- **Resource Efficient**: Uses up to 50 concurrent connections with proper timeout management
- **Memory**: Low memory footprint, suitable for regular use

## Troubleshooting

### No Tapo Devices Found
- Ensure Tapo devices are powered on and connected to the same network
- Check if Windows Firewall is blocking UDP broadcast packets
- Try running as Administrator for elevated network permissions
- Verify devices are not on a guest network or isolated VLAN

### Permission Errors
- Try running as Administrator
- Check Windows Firewall settings for PowerShell/dotnet.exe
- Ensure .NET is properly installed

### Network Access Issues
- Verify network connectivity to the local subnet
- Check if your network uses non-standard IP ranges
- Some corporate networks may block broadcast or multicast traffic

## Building from Source

```cmd
cd NetworkDiscovery
dotnet build
dotnet run
```

## Creating a Standalone Executable

```cmd
cd NetworkDiscovery
dotnet publish -c Release -r win-x64 --self-contained true
```

The executable will be created in `bin/Release/net11.0/win-x64/publish/`

## Technical Implementation

### Tapo Discovery Protocols
- **UDP Broadcast**: Sends discovery packets to 255.255.255.255 on ports 9999/20002
- **JSON Parsing**: Extracts device information from structured responses
- **mDNS Multicast**: Joins multicast group 224.0.0.251 for service discovery
- **Concurrent Processing**: All discovery methods run in parallel for efficiency

### Response Handling
- **Encrypted Responses**: Handles XOR-encrypted and AES-encrypted Tapo payloads
- **Multiple Formats**: Supports various JSON response formats from different firmware versions
- **Fallback Detection**: Non-JSON responses are still captured as potential Tapo devices

## License

This project is provided as-is for educational and network administration purposes. Use responsibly and in accordance with your local network policies and regulations.