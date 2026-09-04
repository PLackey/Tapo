# Tapo MAUI

A .NET MAUI application for controlling TP-Link Tapo smart devices. This is a cross-platform port of the original Rust-based Tapo library, designed to work on Android, iOS, Windows, and macOS.

## Features

- **Device Discovery**: Automatically discover Tapo devices on your local network
- **Device Control**: Turn devices on/off, adjust brightness, change colors
- **Energy Monitoring**: View power consumption and energy usage (for supported devices)
- **Multi-Platform**: Runs on Android, iOS, Windows, and macOS
- **Modern UI**: Built with .NET MAUI using MVVM pattern

## Supported Devices

- **Lights**: L510, L520, L530, L535, L610, L630 (brightness and color control)
- **Light Strips**: L900, L920, L930 (RGB control)
- **Plugs**: P100, P105, P110, P110M, P115 (on/off and energy monitoring)
- **Power Strips**: P300, P304M, P306, P316M (multi-outlet control)
- **Hubs**: H100 (child device management)
- **Cameras**: C210, C220, C225, C325WB, C520WS (basic control)

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 with MAUI workload installed, or Visual Studio Code with C# Dev Kit

### Building and Running

1. Clone the repository
2. Open `TapoMaui.sln` in Visual Studio or the project folder in VS Code
3. Restore NuGet packages
4. Build and run the project

### First Use

1. Enter your Tapo account credentials (username and password)
2. Specify your network range (e.g., 192.168.1.255)
3. Tap "Discover" to find Tapo devices on your network
4. Control your devices through the app interface

## Project Structure

```
TapoMaui/
├── Models/              # Data models and DTOs
├── Services/           # API client and business logic
├── ViewModels/         # MVVM view models
├── Views/              # XAML pages and controls
├── Converters/         # Value converters for data binding
├── Resources/          # App resources (styles, images, fonts)
└── Platforms/          # Platform-specific code
```

## Key Components

### Models
- `DeviceInfo`: Device information and status
- `Color`: Predefined and custom color definitions
- `EnergyUsage`: Power consumption data

### Services
- `ITapoApiClient`: Main interface for Tapo device communication
- `TapoApiClient`: HTTP-based implementation of the Tapo protocol

### ViewModels
- `MainViewModel`: Main page logic and device discovery
- `DeviceItemViewModel`: Individual device control logic

## Authentication

The app uses your Tapo account credentials to authenticate with devices. Credentials are stored locally using MAUI Preferences for convenience. The actual authentication process with Tapo devices involves:

1. Handshake with device
2. RSA key exchange
3. Session token management
4. AES encrypted communication

*Note: The current implementation is simplified for demonstration purposes. A production app would need full protocol implementation including proper encryption.*

## Network Discovery

Device discovery works by:

1. Pinging all IPs in the specified network range
2. Attempting to connect to responsive devices
3. Querying device information to identify Tapo devices
4. Categorizing devices by type and capabilities

## Limitations

- **Simplified Protocol**: This implementation uses a simplified version of the Tapo protocol for demonstration purposes
- **Security**: Production use would require proper implementation of Tapo's security protocols
- **Network Discovery**: Current discovery method is basic; the original Rust library uses more sophisticated UDP broadcast discovery

## Future Enhancements

- Full Tapo protocol implementation with proper encryption
- Advanced scheduling and automation features
- Device grouping and scenes
- Push notifications for device status changes
- Integration with home automation systems

## Contributing

This is a demonstration project converted from the original Rust Tapo library. For production use, consider using the official Tapo app or implementing the full protocol specification.

## License

This project is provided as-is for educational purposes. The original Tapo library is MIT licensed. Please refer to TP-Link's terms of service for device usage guidelines.

## Related Projects

- [Original Tapo Rust Library](https://github.com/mihai-dinculescu/tapo) - The original implementation this project is based on
- [Tapo REST Wrapper](https://github.com/ClementNerma/tapo-rest) - REST API wrapper for the Rust library

## Troubleshooting

### Common Issues

1. **Devices not discovered**: Ensure you're on the same network as your Tapo devices and the correct network range is specified
2. **Connection timeouts**: Check your network configuration and firewall settings
3. **Authentication failures**: Verify your Tapo account credentials are correct

### Platform-Specific Notes

- **Android**: Requires network permissions (automatically included)
- **iOS**: May require additional network configuration for local device access
- **Windows**: Works with both WiFi and Ethernet connections
- **macOS**: Similar to iOS, may need additional network permissions