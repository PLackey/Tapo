# Kiro Development Guide

This document provides guidance for working with this Tapo .NET MAUI project using Kiro, including development workflows, AI-assisted coding patterns, and best practices.

## Project Overview

This project demonstrates a complete conversion from a Rust IoT library to a modern .NET MAUI cross-platform application. It showcases:

- **Cross-platform development** with .NET 10 and MAUI
- **MVVM architecture** with proper separation of concerns  
- **Real-time device control** and monitoring
- **Modern UI patterns** with responsive design

## Development Workflow with Kiro

### 1. Project Structure Understanding

When working with Kiro on this project, focus on these key areas:

```
TapoMaui/
├── Models/              # 📋 Data structures and DTOs
├── Services/           # 🔧 Business logic and API clients
├── ViewModels/         # 🎯 MVVM binding and commands  
├── Views/              # 🎨 XAML UI definitions
├── Converters/         # 🔄 Data binding helpers
├── Resources/          # 🎨 Styles, colors, and assets
└── Platforms/          # 📱 Platform-specific implementations
```

### 2. Common Kiro Tasks

#### Adding New Device Types
When extending support for new Tapo devices:

1. **Update Models** - Add new `DeviceType` enum values and properties
2. **Extend Services** - Add device-specific API methods in `ITapoApiClient`
3. **Update ViewModels** - Add control logic for new device capabilities
4. **Modify UI** - Add appropriate controls in device views

Example prompt for Kiro:
```
Add support for Tapo T110 temperature sensor. Include temperature reading, 
battery level monitoring, and alert configuration. Follow the existing 
pattern for device discovery and control.
```

#### UI/UX Improvements
For enhancing the user interface:

```
Improve the device discovery UI with a progress indicator showing scan 
progress, found device count, and estimated time remaining. Add pull-to-refresh 
functionality and better error messaging.
```

#### Performance Optimization
When addressing performance issues:

```
Optimize the device discovery process to scan devices in parallel batches 
of 10, add connection pooling for HTTP requests, and implement proper 
cancellation token support.
```

### 3. Architecture Patterns

This project demonstrates several key patterns that Kiro can help extend:

#### MVVM with Source Generators
```csharp
[ObservableProperty]
private bool _isConnected;

[RelayCommand]
private async Task RefreshAsync()
{
    // Kiro can help generate similar patterns
}
```

#### Dependency Injection
```csharp
// Services are registered in MauiProgram.cs
builder.Services.AddSingleton<ITapoApiClient, TapoApiClient>();
```

#### Async/Await Patterns
```csharp
// All device operations are async to prevent UI blocking
await device.SetBrightnessAsync(brightness);
```

### 4. Testing Strategy

#### Unit Testing
Focus on testing:
- **Service layer logic** (API client methods)
- **ViewModel behavior** (command execution, property changes)  
- **Data conversion** (value converters, model mapping)

#### Integration Testing
Test:
- **Device communication** with mock devices
- **Cross-platform compatibility** 
- **UI responsiveness** under load

#### Example Kiro prompt for testing:
```
Create comprehensive unit tests for the DeviceItemViewModel class, 
including tests for device state changes, command execution, and 
error handling scenarios. Use MOQ for mocking dependencies.
```

### 5. Platform-Specific Development

#### Android
```csharp
// Platform-specific network permission handling
#if ANDROID
[assembly: UsesPermission(Android.Manifest.Permission.AccessNetworkState)]
#endif
```

#### iOS  
```csharp
// iOS-specific local network access
#if IOS
// Add network usage description to Info.plist
#endif
```

#### Windows
```csharp
// Windows-specific features like live tiles
#if WINDOWS
// Implement Windows-specific functionality
#endif
```

### 6. Code Quality Guidelines

#### Naming Conventions
- **Classes**: PascalCase (`TapoApiClient`)
- **Methods**: PascalCase (`GetDeviceInfoAsync`)  
- **Properties**: PascalCase (`DeviceInfo`)
- **Fields**: camelCase with underscore (`_isConnected`)
- **Constants**: UPPER_CASE (`MAX_RETRY_COUNT`)

#### Error Handling
```csharp
try
{
    var result = await _apiClient.GetDeviceInfoAsync(ipAddress);
    return result;
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "Network error connecting to device {IP}", ipAddress);
    throw new DeviceConnectionException("Failed to connect to device", ex);
}
```

#### Resource Management
```csharp
// Proper disposal of resources
public void Dispose()
{
    _httpClient?.Dispose();
    _cancellationTokenSource?.Dispose();
}
```

### 7. Common Kiro Prompts

#### Feature Development
```
Add support for device scheduling. Users should be able to set timers 
for devices to turn on/off at specific times or after delays. Include 
recurring schedule support and timezone handling.
```

#### Bug Fixing  
```
The device discovery is sometimes missing devices on slower networks. 
Investigate timeout issues and implement retry logic with exponential 
backoff. Add network diagnostics to help users troubleshoot.
```

#### Code Review
```
Review the TapoApiClient class for potential improvements. Look for 
opportunities to reduce code duplication, improve error handling, 
and enhance performance.
```

#### Documentation
```
Create comprehensive XML documentation comments for the ITapoApiClient 
interface and all public methods. Include parameter descriptions, 
return value details, and usage examples.
```

### 8. Deployment and Distribution

#### Build Configuration
```xml
<!-- Release configuration for app stores -->
<PropertyGroup Condition="'$(Configuration)'=='Release'">
    <AndroidPackageFormat>aab</AndroidPackageFormat>
    <RuntimeIdentifier Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">android-arm64</RuntimeIdentifier>
</PropertyGroup>
```

#### Store Preparation
When preparing for app store submission:

```
Prepare the app for Microsoft Store submission. Update the app manifest 
with proper metadata, create store screenshots, and ensure compliance 
with store policies. Include privacy policy and terms of service.
```

### 9. Advanced Features to Implement

#### Real-time Updates
```
Implement SignalR or WebSocket connections for real-time device status 
updates without polling. Include proper connection management and 
reconnection logic.
```

#### Offline Support
```
Add offline capabilities with local SQLite database for caching device 
states, queuing commands when network is unavailable, and syncing when 
connection is restored.
```

#### Analytics and Telemetry
```
Integrate Application Insights for crash reporting and usage analytics. 
Include custom telemetry for device usage patterns while respecting 
user privacy preferences.
```

### 10. Best Practices with Kiro

#### Incremental Development
- Start with small, focused changes
- Test each feature thoroughly before moving to the next
- Use Kiro to generate boilerplate code and focus on business logic

#### Code Documentation
- Ask Kiro to generate XML documentation for public APIs
- Create inline comments for complex algorithms
- Maintain up-to-date README and architectural documentation

#### Refactoring
- Use Kiro to identify code duplication and suggest improvements
- Regularly review and optimize performance-critical code paths
- Keep dependencies up to date and remove unused packages

## Conclusion

This Tapo .NET MAUI project serves as an excellent example of modern cross-platform development practices. Use Kiro to:

- **Accelerate development** by generating boilerplate code
- **Improve code quality** through automated reviews and suggestions  
- **Learn new patterns** by exploring different implementation approaches
- **Solve complex problems** by breaking them down into manageable tasks

The combination of .NET 10, MAUI, and AI-assisted development with Kiro provides a powerful platform for creating sophisticated IoT applications that work seamlessly across all major platforms.

---

*Happy coding with Kiro! 🚀*