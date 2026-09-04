# Enhanced Network Discovery Tool with Tapo Detection

A comprehensive .NET console application that discovers devices on your local network with specialized detection for TP-Link Tapo smart devices.

## 🎯 Key Features

### General Network Discovery
- **Network Interface Detection**: Automatically detects active ethernet and Wi-Fi interfaces
- **IP Range Scanning**: Scans entire /24 subnets (254 addresses) for active devices
- **Device Identification**: Attempts to identify device types based on open ports
- **Port Scanning**: Scans common ports to determine device services
- **Hostname Resolution**: Resolves hostnames when possible
- **Response Time Measurement**: Shows ping response times for discovered devices

### 🔌 Tapo-Specific Discovery

#### UDP Broadcast Discovery
- **Port 9999**: Discovers Tapo smart plugs, bulbs, and switches
- **Port 20002**: Discovers Tapo cameras and security devices
- **JSON Response Parsing**: Extracts detailed device information including:
  - Device model (P110, C200, etc.)
  - MAC address and device ID
  - Hardware and software versions
  - Device aliases and nicknames
  - Device type classification

#### mDNS Discovery
- **HomeKit Compatible Devices**: Finds Tapo devices with HomeKit support
- **Matter Protocol**: Discovers newer Matter-compatible Tapo devices
- **Service Discovery**: Identifies Tapo-specific network services
- **TP-Link Service Detection**: Finds devices broadcasting TP-Link services

## 🔍 **How Tapo Discovery Works**

### 1. UDP Broadcast Discovery (Primary Method)
The tool sends specific UDP broadcast packets to your local subnet (255.255.255.255):

#### **Discovery Process:**
1. **Bind to Local Port**: Creates a UDP client on an available local port for responses
2. **Send Broadcast Packets**: Sends multiple discovery payload formats to ports 9999/20002
3. **Listen for Unicast Responses**: Tapo devices respond directly back to the sender's IP
4. **Parse JSON Responses**: Extracts device information from structured responses

#### **Discovery Payloads Used:**
- `{"system":{"get_sysinfo":{}}}` - TP-Link standard format
- `{"method":"get_device_info","params":{}}` - Tapo camera format  
- `{"method":"handshake","params":{"key":"","requestTimeMils":0}}` - App format
- `{"method":"get_device_usage"}` - Usage query format
- `{}` - Minimal JSON (some devices respond to any JSON)

#### **Response Handling:**
- **JSON Responses**: Parsed for device model, MAC, firmware, aliases
- **Non-JSON Responses**: Flagged as potential encrypted Tapo devices
- **Unicast Replies**: Devices respond directly to sender (not broadcast)
- **Multiple Formats**: Handles various JSON structures from different firmware versions

### 2. mDNS Discovery (Secondary Method)
For Tapo devices with smart home protocol support:
- **HomeKit Services** (`_hap._tcp.local`): Tapo devices with HomeKit support
- **Matter Services** (`_matterc._udp.local`): Newer Matter-compatible devices
- **HTTP Services** (`_http._tcp.local`): Web interface services
- **TP-Link Services** (`_tplink._tcp.local`): Manufacturer-specific services

### 3. Enhanced Device Detection
The tool identifies device types based on model numbers:
- **P1xx models** → Smart Plugs (P100, P110, P115)
- **L5xx models** → Smart Bulbs (L510, L530)
- **L9xx models** → Light Strips (L900, L920)
- **C2xx/C3xx models** → Security Cameras (C200, C310)
- **D1xx/D2xx models** → Video Doorbells (D130, D230)
- **S2xx models** → Smart Switches (S200, S220)
- **H1xx/H2xx models** → Smart Hubs (H100, H200)
- **T1xx/T3xx models** → Sensors (T100, T300)

## Device Types Detected

### Tapo Devices
- **Smart Plugs**: P100, P110, P115 (with energy monitoring)
- **Smart Bulbs**: L510, L530, L900 series
- **Smart Switches**: S200B, S220
- **Security Cameras**: C200, C210, C220, C310, C320WS
- **Doorbells**: D230S1
- **Hubs**: H100, H200

### Other Network Devices
- **Routers/Gateways**: Web servers with DNS (ports 80, 443, 53)
- **Windows Computers**: RPC and file sharing services (ports 135, 139, 445)
- **Linux/Unix Systems**: SSH servers (port 22)
- **Network Printers**: Print services (ports 631, 9100)
- **Apple Devices**: AFP or Bonjour services (ports 548, 5353)
- **IoT Devices**: UPnP services (port 1900)

## Usage

### Running the Application

```bash
cd NetworkDiscovery
dotnet run
```

### Building the Application

```bash
cd NetworkDiscovery
dotnet build
```

### Creating a Portable Executable

```bash
cd NetworkDiscovery
dotnet publish -c Release -r win-x64 --self-contained true
```

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
Found: 192.168.1.50 (DESKTOP-ABC123) - 5ms

Network Discovery Results:
==========================

🔌 TAPO DEVICES FOUND (1):
═══════════════════════════════
📍 IP Address: 192.168.1.105
   Device Type: SMART.TAPOPLUG
   Model: P110
   Discovery Method: UDP Broadcast (Port 9999)
   MAC Address: AA:BB:CC:DD:EE:FF
   Device ID: abc123def456
   Alias: Living Room Lamp
   Hardware Version: 1.0
   Software Version: 1.2.3
   Port: 9999

🌐 OTHER NETWORK DEVICES (2):
═══════════════════════════════════════
📍 IP Address: 192.168.1.1
   Hostname: router.home
   Device Type: Web Server
   Response Time: 2ms
   Open Ports: 53, 80, 443

📍 IP Address: 192.168.1.50
   Hostname: DESKTOP-ABC123
   Device Type: Windows RPC
   Response Time: 5ms
   Open Ports: 135, 139, 445

═══════════════════════════════════════
📊 DISCOVERY SUMMARY:
   • Tapo Devices: 1
   • Other Devices: 2
   • Total Devices: 3
```

## Performance Considerations

- **Concurrent Scanning**: Uses up to 50 simultaneous connections for network scanning
- **Optimized Timeouts**: 
  - Ping timeout: 1000ms
  - Port scan timeout: 500ms per port
  - UDP discovery timeout: 3 seconds
  - mDNS discovery timeout: 5 seconds
- **Efficient Discovery**: Tapo-specific discovery runs in parallel with general network scanning
- **Memory Efficient**: Low memory footprint suitable for regular use

## Requirements

- **.NET 11.0** or later
- **Network Access**: Tool needs to send ping, TCP, UDP broadcast, and multicast requests
- **Windows**: Optimized for Windows, but cross-platform compatible
- **Firewall Permissions**: May require administrator privileges for broadcast operations

## Security Considerations

- ⚠️ **Use Responsibly**: Only scan networks you own or have explicit permission to scan
- 🛡️ **Network Security**: Some security systems may detect and log scanning activity
- 🔒 **Broadcast Traffic**: Uses UDP broadcast which may be monitored by network security
- 📊 **Non-Intrusive**: Doesn't attempt to exploit or access devices, only identifies them
- 🔐 **Encryption Aware**: Handles both encrypted and unencrypted Tapo responses

## Troubleshooting

### No Tapo Devices Found
- Ensure Tapo devices are powered on and connected to the same network
- Check if Windows Firewall is blocking UDP broadcast packets
- Try running as Administrator for elevated network permissions
- Verify devices are not on a guest network or isolated VLAN

### UDP Discovery Issues
- Some routers may block broadcast traffic between devices
- Try connecting to the same wireless network as the devices
- Check router settings for device isolation or AP isolation features

### mDNS Discovery Problems
- Ensure multicast traffic is allowed on your network
- Some corporate networks disable multicast DNS
- HomeKit/Matter features may need to be enabled on devices

### Permission Errors
- Run PowerShell or Command Prompt as Administrator
- Check Windows Defender Firewall settings
- Ensure .NET runtime is properly installed

## Technical Implementation

### Discovery Methods
1. **UDP Broadcast**: Sends discovery packets to broadcast address (255.255.255.255)
2. **mDNS Multicast**: Joins multicast group (224.0.0.251) and queries for services
3. **JSON Parsing**: Extracts device information from Tapo response payloads
4. **Service Detection**: Identifies device types based on network service patterns

### Supported Tapo Protocols
- **Basic Discovery**: Simple UDP broadcast with JSON responses
- **Encrypted Discovery**: Handles XOR-encrypted and AES-encrypted payloads
- **mDNS Services**: HomeKit, Matter, and TP-Link specific service types
- **Port-based Detection**: Identifies devices by characteristic port patterns

## License

This project is provided as-is for educational and network administration purposes. Use responsibly and in accordance with your local network policies and regulations.