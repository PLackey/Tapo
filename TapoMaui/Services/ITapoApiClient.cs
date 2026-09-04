using TapoMaui.Models;
using DeviceInfo = TapoMaui.Models.DeviceInfo;
using DeviceType = TapoMaui.Models.DeviceType;
using Color = TapoMaui.Models.Color;

namespace TapoMaui.Services;

public interface ITapoApiClient
{
    Task<IEnumerable<DiscoveredDevice>> DiscoverDevicesAsync(string targetNetwork = "", int timeoutSeconds = 10);
    Task<DeviceInfo> GetDeviceInfoAsync(string ipAddress);
    Task<bool> TurnOnAsync(string ipAddress);
    Task<bool> TurnOffAsync(string ipAddress);
    Task<bool> SetBrightnessAsync(string ipAddress, int brightness);
    Task<bool> SetColorAsync(string ipAddress, Color color);
    Task<bool> SetHueSaturationAsync(string ipAddress, int hue, int saturation);
    Task<bool> SetColorTemperatureAsync(string ipAddress, int colorTemp);
    Task<EnergyUsage?> GetEnergyUsageAsync(string ipAddress);
    Task<CurrentPower?> GetCurrentPowerAsync(string ipAddress);
    
    // Add methods to set credentials dynamically
    void SetCredentials(string username, string password);
    string GetUsername();
    string GetPassword();
}

public interface ITapoDevice
{
    string IpAddress { get; }
    DeviceInfo DeviceInfo { get; }
    DeviceType DeviceType { get; }
    
    Task<bool> OnAsync();
    Task<bool> OffAsync();
    Task<DeviceInfo> GetDeviceInfoAsync();
}

public interface ITapoLightDevice : ITapoDevice
{
    Task<bool> SetBrightnessAsync(int brightness);
}

public interface ITapoColorLightDevice : ITapoLightDevice
{
    Task<bool> SetColorAsync(Color color);
    Task<bool> SetHueSaturationAsync(int hue, int saturation);
    Task<bool> SetColorTemperatureAsync(int colorTemp);
}

public interface ITapoEnergyMonitoringDevice : ITapoDevice
{
    Task<EnergyUsage?> GetEnergyUsageAsync();
    Task<CurrentPower?> GetCurrentPowerAsync();
}