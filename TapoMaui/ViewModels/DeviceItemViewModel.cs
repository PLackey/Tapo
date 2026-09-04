using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TapoMaui.Models;
using TapoMaui.Services;
using Color = TapoMaui.Models.Color;
using DeviceType = TapoMaui.Models.DeviceType;

namespace TapoMaui.ViewModels;

public partial class DeviceItemViewModel : BaseViewModel
{
    private readonly ITapoApiClient _tapoClient;
    
    [ObservableProperty]
    private DiscoveredDevice _device;
    
    [ObservableProperty]
    private bool _isOn;
    
    [ObservableProperty]
    private int _brightness = 50;
    
    [ObservableProperty]
    private Color _selectedColor = Color.Red;
    
    [ObservableProperty]
    private int _colorTemperature = 2700;
    
    [ObservableProperty]
    private string _currentPower = "N/A";
    
    [ObservableProperty]
    private string _energyUsage = "N/A";

    public DeviceItemViewModel(ITapoApiClient tapoClient, DiscoveredDevice device)
    {
        _tapoClient = tapoClient;
        _device = device;
        _isOn = device.DeviceInfo.DeviceOn;
        _brightness = device.DeviceInfo.Brightness ?? 50;
        
        Title = device.DeviceInfo.Nickname ?? device.DeviceInfo.Model;
        
        // Load initial data
        _ = Task.Run(LoadDeviceDataAsync);
    }
    
    public bool IsColorDevice => Device.DeviceType == DeviceType.ColorLight || 
                                Device.DeviceType == DeviceType.RgbLightStrip || 
                                Device.DeviceType == DeviceType.RgbicLightStrip;
    
    public bool IsEnergyMonitoringDevice => Device.DeviceType == DeviceType.PlugEnergyMonitoring || 
                                           Device.DeviceType == DeviceType.PowerStripEnergyMonitoring;
    
    public bool HasBrightness => Device.DeviceType != DeviceType.Plug && 
                                Device.DeviceType != DeviceType.PlugEnergyMonitoring;
                                
    public List<Color> AvailableColors => Color.GetPredefinedColors();

    [RelayCommand]
    private async Task TogglePowerAsync()
    {
        if (IsBusy) return;
        
        try
        {
            IsBusy = true;
            ClearError();
            
            bool success;
            if (IsOn)
            {
                success = await _tapoClient.TurnOffAsync(Device.IpAddress);
            }
            else
            {
                success = await _tapoClient.TurnOnAsync(Device.IpAddress);
            }
            
            if (success)
            {
                IsOn = !IsOn;
            }
            else
            {
                ShowError("Failed to toggle device power");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error toggling power: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    [RelayCommand]
    private async Task SetBrightnessAsync(int brightness)
    {
        if (IsBusy || !HasBrightness) return;
        
        try
        {
            IsBusy = true;
            ClearError();
            
            var success = await _tapoClient.SetBrightnessAsync(Device.IpAddress, brightness);
            
            if (success)
            {
                Brightness = brightness;
            }
            else
            {
                ShowError("Failed to set brightness");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error setting brightness: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    [RelayCommand]
    private async Task SetColorAsync(Color color)
    {
        if (IsBusy || !IsColorDevice) return;
        
        try
        {
            IsBusy = true;
            ClearError();
            
            var success = await _tapoClient.SetColorAsync(Device.IpAddress, color);
            
            if (success)
            {
                SelectedColor = color;
            }
            else
            {
                ShowError("Failed to set color");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error setting color: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    [RelayCommand]
    private async Task SetColorTemperatureAsync(int temperature)
    {
        if (IsBusy || !IsColorDevice) return;
        
        try
        {
            IsBusy = true;
            ClearError();
            
            var success = await _tapoClient.SetColorTemperatureAsync(Device.IpAddress, temperature);
            
            if (success)
            {
                ColorTemperature = temperature;
            }
            else
            {
                ShowError("Failed to set color temperature");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error setting color temperature: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        await LoadDeviceDataAsync();
    }
    
    private async Task LoadDeviceDataAsync()
    {
        if (IsBusy) return;
        
        try
        {
            IsBusy = true;
            ClearError();
            
            // Update device info
            var deviceInfo = await _tapoClient.GetDeviceInfoAsync(Device.IpAddress);
            if (deviceInfo != null)
            {
                Device.DeviceInfo = deviceInfo;
                IsOn = deviceInfo.DeviceOn;
                if (deviceInfo.Brightness.HasValue)
                    Brightness = deviceInfo.Brightness.Value;
            }
            
            // Load energy data if supported
            if (IsEnergyMonitoringDevice)
            {
                var currentPower = await _tapoClient.GetCurrentPowerAsync(Device.IpAddress);
                if (currentPower != null)
                {
                    CurrentPower = $"{currentPower.Power}W";
                }
                
                var energyUsage = await _tapoClient.GetEnergyUsageAsync(Device.IpAddress);
                if (energyUsage != null)
                {
                    EnergyUsage = $"Today: {energyUsage.TodayEnergy}Wh";
                }
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error loading device data: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}