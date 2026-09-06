# Tapo .NET MAUI

A .NET MAUI application for controlling TP-Link Tapo smart devices built with .NET 10. This cross-platform application provides an intuitive interface for managing your smart home devices across Android, iOS, Windows, and macOS.

## Features

- **Device Discovery**: Automatically discover Tapo devices on your local network using multiple methods
- **Device Control**: Turn devices on/off, adjust brightness, change colors
- **Video Streaming**: Live RTSP video streaming from Tapo cameras with full-screen player
- **Energy Monitoring**: View power consumption and energy usage (for supported devices)
- **Cross-Platform**: Runs on Android, iOS, Windows, and macOS with native performance
- **Modern UI**: Responsive design with dark/light theme support and touch-friendly controls

## Supported Devices

- **Smart Lights**: L510, L520, L530, L535, L610, L630 (brightness and color control)
- **Light Strips**: L900, L920, L930 (RGB control)
- **Smart Plugs**: P100, P105, P110, P110M, P115 (on/off and energy monitoring)
- **Power Strips**: P300, P304M, P306, P316M (multi-outlet control)
- **Hubs**: H100 (child device management)

## ⚖️ Legal Disclaimer

**🚨 IMPORTANT:** This software is provided "AS IS" with **NO WARRANTY**. See [DISCLAIMER.md](DISCLAIMER.md) for complete legal terms including:

- **US & UK warranty disclaimers** and liability limitations
- **Network security** and privacy responsibilities  
- **RTSP streaming** security considerations
- **Regulatory compliance** requirements

**⚠️ Use at your own risk.** Developer not liable for device damage, data loss, privacy breaches, or network security issues.
- **Cameras**: C210, C220, C225, C325WB, C520WS, TC40, TC70 (basic control)

## Requirements

- **.NET 10 SDK** or later
- **Visual Studio 2022** (version 17.12 or later) with .NET MAUI workload installed
- **Alternative**: Visual Studio Code with C# Dev Kit extension

## Getting Started

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd tapo
   ```

2. **Open the solution**
   - Open `TapoMaui.sln` in Visual Studio 2022
   - Or open the project folder in Visual Studio Code

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

4. **Build the project**
   ```bash
   dotnet build
   ```

5. **Run the application**
   - Select your target platform (Android, iOS, Windows, or macOS)
   - Press F5 or click "Start Debugging"

### First Use

1. **Discover Devices**: Simply tap the "Discover" button - no network configuration needed!
2. **Auto-Network Detection**: The app automatically detects and scans all local networks
3. **Create Local Camera Account** (for camera control):
   - Open the **Tapo mobile app** on your phone
   - Tap your camera to go to its live view
   - Tap the **gear icon** in the top right corner (Camera Settings)
   - Scroll down and select **Advanced Settings** or **Camera Account**
   - Create your own custom username and password for local streaming
4. **Enter Camera Credentials**: Input the local camera username/password you just created
5. **Control Devices**: Use the interface to control your discovered devices

**Important**: Tapo cameras do NOT have default credentials (like admin/admin). You MUST create a local "Camera Account" in the Tapo mobile app first before you can control cameras from third-party applications.

**Note**: The network field is completely optional. Leave it empty for automatic network detection across all interfaces, or specify a particular network (like `192.168.7.255`) to limit the scan scope.

## Troubleshooting Tools

### NetworkDiscovery Console Tool

The `Tools/NetworkDiscovery/` directory contains a powerful console application for diagnosing network discovery issues. This tool is particularly useful if the main MAUI app isn't finding your Tapo devices.

**Features:**
- **Multiple Discovery Methods**: UDP broadcast, mDNS, TCP port scanning, and HTTPS detection
- **Detailed Logging**: Shows exactly how devices are detected and which methods work
- **Network Analysis**: Identifies all network interfaces and scans appropriate ranges
- **Tapo-Specific Detection**: Uses the same detection logic as the MAUI app for consistency

**Usage:**
```bash
cd Tools/NetworkDiscovery
dotnet build
dotnet run

# Or run the executable directly:
cd bin/Debug/net11.0
./NetworkDiscovery.exe
```

**Sample Output:**
```
Enhanced Network Discovery Tool with Tapo Detection
==================================================

Active Network Interfaces:
  192.168.7.114

🔍 Found HTTPS port 443 on 192.168.7.103
🔍 Found TP-Link port 8800 on 192.168.7.103
✅ Found Tapo camera via HTTPS: 192.168.7.103

TAPO DEVICES FOUND (1):
═══════════════════════════
📹 IP Address: 192.168.7.103
   Device Type: Tapo Camera with Streaming
   Discovery Method: HTTPS Port Scan
   Port: 443
```

**When to Use:**
- The MAUI app shows "No Tapo devices found"
- You know devices exist on your network but they're not being detected
- You want to verify which discovery methods work for your specific devices
- Network connectivity troubleshooting

The NetworkDiscovery tool uses the same TCP/HTTP detection methods as the MAUI app, so if it finds devices that the app doesn't, it indicates a configuration issue rather than a network problem.

## Tapo Camera Setup

### Important: No Default Credentials

TP-Link Tapo cameras **do not have universal default credentials** (like "admin/admin") for local network access. This is a security feature that requires manual setup.

### Creating Local Camera Credentials

To control Tapo cameras from this app (or any third-party software like VLC, Blue Iris, NVR, or NAS), you must first create a local "Camera Account":

1. **Open Tapo Mobile App**: Use the official Tapo app on your smartphone
2. **Select Your Camera**: Tap the camera you want to configure
3. **Camera Settings**: Tap the gear icon (⚙️) in the top right corner  
4. **Advanced Settings**: Scroll down and select "Advanced Settings" or "Camera Account"
5. **Create Account**: Set up your own custom username and password for local access

### Local Stream Access

Once you've created the local account, you can access camera streams using:

**High Quality Stream:**
```
rtsp://username:password@IP-Address:554/stream1
```

**Standard Quality Stream:**  
```
rtsp://username:password@IP-Address:554/stream2
```

**HTTPS Control Interface:**
```
https://IP-Address:443/
```

### Security Best Practices

- Use a strong, unique password for your camera account
- Different from your TP-Link cloud account credentials  
- Consider network segmentation for camera traffic
- Regularly update camera firmware through the Tapo app

### Video Streaming Features

**Live Camera Streaming:**
- **RTSP Protocol**: High-quality video streaming over local network
- **Multiple Resolutions**: Automatic quality selection (1080p preferred)
- **Full-Screen Player**: Dedicated video player page with media controls
- **Stream Controls**: Start/stop streaming with live status indicators
- **Secure Authentication**: Uses local camera credentials for stream access

**Stream URLs Used:**
- High Quality: `rtsp://username:password@camera-ip:554/stream1`
- Standard Quality: `rtsp://username:password@camera-ip:554/stream2` (fallback)

**Camera Controls:**
- "Start/Stop Stream" - Toggle streaming on/off
- "View Stream" - Open full-screen video player
- Live status indicator (🔴 Live / ⚫ Offline)

## Architecture

### Technology Stack
- **.NET 10**: Latest .NET framework with improved performance and new features
- **MAUI**: Cross-platform UI framework for native mobile and desktop apps
- **MVVM Pattern**: Clean separation of concerns with data binding
- **CommunityToolkit.Mvvm**: Modern MVVM helpers with source generators
- **HttpClient**: HTTP communication with Tapo devices
- **System.Text.Json**: High-performance JSON serialization

### Project Structure
```
TapoMaui/
├── Models/              # Device data models and DTOs
├── Services/           # API client and HTTP communication
├── ViewModels/         # MVVM pattern view models
├── Views/              # XAML UI pages and controls
├── Converters/         # Data binding value converters
├── Resources/          # Styles, fonts, icons, and assets
└── Platforms/          # Platform-specific implementations
```

### Key Features Implementation

#### Device Discovery
- **Auto Network Detection**: Automatically detects all active network interfaces (WiFi and Ethernet)
- **Comprehensive Scanning**: Uses multiple discovery methods concurrently:
  - **UDP Broadcast**: Traditional Tapo discovery on ports 9999 and 20002
  - **mDNS Discovery**: Service discovery for network-advertised devices
  - **TCP Port Scanning**: Multi-port detection (443, 2020, 8800, 554) like cameras
  - **HTTP/HTTPS Probing**: Validates Tapo device signatures and capabilities
  - **Discovery Probes**: Sends activation packets to wake up dormant device APIs
- **No Configuration Required**: Works without specifying network ranges
- **Multiple Network Support**: Scans all detected networks simultaneously
- **Fallback Networks**: Includes common network ranges if auto-detection fails

#### Device Control
- **Universal Controls**: Power on/off for all device types
- **Light Management**: Brightness, color, and color temperature control
- **Energy Monitoring**: Real-time power consumption and usage statistics
- **Batch Operations**: Control multiple devices simultaneously

#### User Interface
- **Responsive Design**: Adapts to different screen sizes and orientations
- **Theme Support**: Automatic dark/light mode switching
- **Touch-Friendly**: Optimized for mobile touch interactions
- **Accessibility**: Screen reader and accessibility feature support

## Platform Support

| Platform | Status | Features |
|----------|--------|----------|
| **Android** | ✅ Full Support | All features, network permissions included |
| **iOS** | ✅ Full Support | Complete functionality with local network access |
| **Windows** | ✅ Full Support | Desktop application with full feature set |
| **macOS** | ✅ Full Support | Native Mac application support |

## Development

### Prerequisites
- **Visual Studio 2022** (17.12+) with .NET MAUI workload
- **.NET 10 SDK** installed
- **Platform SDKs** for target platforms (Android SDK, Xcode for iOS/Mac)

### Building from Source
```bash
# Clone the repository
git clone <repository-url>
cd tapo

# Restore NuGet packages
dotnet restore

# Build for specific platform
dotnet build -f net10.0-android    # Android
dotnet build -f net10.0-ios        # iOS
dotnet build -f net10.0-windows    # Windows
dotnet build -f net10.0-maccatalyst # macOS
```

### Running Tests
```bash
dotnet test
```

### Contributing
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## Configuration

### Network Settings
- **Default Range**: 192.168.1.255 (scans 192.168.1.1-254)
- **Custom Networks**: Support for different subnet ranges
- **Timeout Settings**: Configurable discovery and connection timeouts

### Security
- **Credential Storage**: Secure storage using platform keychain/credential manager
- **Local Communication**: All communication stays on local network
- **No Cloud Dependency**: Direct device communication without cloud services

## Tools Directory

The project includes a comprehensive **Network Discovery Tool** located in the `Tools/` directory that provides advanced troubleshooting capabilities for Tapo device discovery issues.

### 🔧 Enhanced Network Discovery Tool

Located in `Tools/NetworkDiscovery/`, this .NET console application offers:

#### **Specialized Tapo Device Discovery**
- **UDP Broadcast Discovery** on ports 9999 (smart plugs/bulbs) and 20002 (cameras)
- **mDNS Service Discovery** for HomeKit and Matter-compatible devices
- **ONVIF Camera Detection** for advanced camera discovery
- **Multi-method Discovery** using various discovery protocols simultaneously

#### **Advanced Detection Features**
- **Device Classification** automatically identifies plugs, bulbs, cameras, and switches
- **Detailed Device Information** extracts model numbers, MAC addresses, firmware versions
- **Port Scanning** identifies Tapo devices by their network service signatures
- **Encrypted Response Handling** processes both encrypted and plain-text device responses

#### **Quick Usage**
```bash
# Navigate to project root and run:
Tools\run-discovery.bat

# Or use PowerShell:
Tools\run-discovery.ps1

# Or manually:
cd Tools\NetworkDiscovery
dotnet run
```

#### **When to Use the Tool**
- **Camera Discovery Issues**: If your cameras aren't appearing in the main app
- **Network Troubleshooting**: To identify all Tapo devices on your network
- **Device Information**: To gather detailed device specs and configuration
- **Connectivity Testing**: To verify network configuration and firewall settings

The tool provides detailed output showing discovery methods, device responses, and network diagnostics that can help resolve discovery issues in the main application.

## Troubleshooting

### Common Issues

**Camera Control Issues:**
If device discovery works but camera control fails:
1. ✅ **Discovery working** = Camera is on network and responding
2. ❌ **Control failing** = Local camera account not created or wrong credentials
3. 🔧 **Solution** = Create/verify local camera account in Tapo mobile app

**Video Streaming Issues:**
If camera is discovered but streaming fails:
1. ✅ **Camera found** = Network communication working
2. ❌ **Stream failing** = Authentication or RTSP connectivity issue
3. 🔧 **Solutions**:
   - Verify local camera credentials are entered correctly
   - Check if RTSP is enabled in camera settings (usually enabled by default)
   - Ensure port 554 is not blocked by firewall
   - Try both high quality (stream1) and standard quality (stream2) URLs
   - Test stream URL in VLC player: `rtsp://username:password@camera-ip:554/stream1`

1. **No Devices Found**
   - Use the **Network Discovery Tool** in `Tools/` directory to diagnose discovery issues
   - Ensure devices are on the same network
   - Check network range configuration
   - Verify Tapo account credentials
   - Run `Tools\run-discovery.bat` to see detailed discovery diagnostics

2. **Camera Not Discovered** *(Known Issue)*
   - Use the Network Discovery Tool to identify camera IP and model
   - Cameras may require different discovery protocols (UDP port 20002, ONVIF, HTTPS)
   - Check if camera is on port 2020 (ONVIF) or 8800 (TP-Link streaming)
   - Verify camera firmware supports local API access

3. **Connection Timeouts**
   - Check firewall settings
   - Ensure devices are powered on and connected
   - Try reducing network scan range
   - Use the Network Discovery Tool to test specific IP addresses

4. **Control Issues**
   - Verify device compatibility using the discovery tool's device classification
   - Check device firmware version
   - Restart the application

### Platform-Specific Notes

#### Android
- Requires `ACCESS_NETWORK_STATE` and `INTERNET` permissions (automatically granted)
- May need manual network permission on Android 10+

#### iOS
- Requires local network permission prompt
- May need "Allow on Local Network" permission in Settings

#### Windows
- Works with both WiFi and Ethernet connections
- Windows Defender may require network access permission

## Performance Optimization

### .NET 10 Improvements
- **AOT Compilation**: Faster startup and reduced memory usage
- **Improved GC**: Better garbage collection for mobile devices
- **MAUI Performance**: Enhanced rendering and layout performance
- **Native Interop**: Optimized platform-specific code execution

### Best Practices
- **Async Operations**: All network calls use async/await patterns
- **Memory Management**: Proper disposal of resources and subscriptions
- **UI Responsiveness**: Background threading for intensive operations
- **Battery Optimization**: Efficient polling and network usage

## API Reference

### Core Services

#### ITapoApiClient
Primary interface for device communication:
```csharp
Task<IEnumerable<DiscoveredDevice>> DiscoverDevicesAsync(string targetNetwork, int timeoutSeconds);
Task<DeviceInfo> GetDeviceInfoAsync(string ipAddress);
Task<bool> TurnOnAsync(string ipAddress);
Task<bool> TurnOffAsync(string ipAddress);
```

#### Device Models
- **DeviceInfo**: Device status and capabilities
- **DiscoveredDevice**: Network discovery results  
- **Color**: RGB and HSV color definitions
- **EnergyUsage**: Power consumption statistics

### ViewModels

#### MainViewModel
Main application logic:
- Device discovery management
- Credential handling
- Network configuration

#### DeviceItemViewModel  
Individual device control:
- Power state management
- Property adjustments (brightness, color)
- Real-time status updates

## Roadmap

### v1.1 - Enhanced Features
- [ ] Device scheduling and timers
- [ ] Scene management (save/recall device states)
- [ ] Bulk device operations
- [ ] Export/import device configurations

### v1.2 - Advanced Integration
- [ ] Home Assistant integration
- [ ] Voice control support
- [ ] Geofencing automation
- [ ] Push notifications

### v1.3 - Enterprise Features
- [ ] Multi-user support
- [ ] Role-based access control
- [ ] Audit logging
- [ ] Cloud synchronization

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Inspired by the original [Tapo Rust library](https://github.com/mihai-dinculescu/tapo)
- Built with [.NET MAUI](https://dotnet.microsoft.com/apps/maui)
- UI components from [CommunityToolkit](https://github.com/CommunityToolkit)

## Related Projects

- [Original Tapo Rust Library](https://github.com/mihai-dinculescu/tapo) - The original implementation
- [Tapo REST Wrapper](https://github.com/ClementNerma/tapo-rest) - REST API wrapper for the Rust library

---

*Note: This is an unofficial implementation. TP-Link and Tapo are trademarks of TP-Link Technologies Co., Ltd.*
