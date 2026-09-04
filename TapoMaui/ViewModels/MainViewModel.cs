using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TapoMaui.Models;
using TapoMaui.Services;

namespace TapoMaui.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly ITapoApiClient _tapoClient;
    
    [ObservableProperty]
    private string _username = string.Empty;
    
    [ObservableProperty]
    private string _password = string.Empty;
    
    [ObservableProperty]
    private string _networkTarget = "";
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DiscoverDevicesCommand))]
    private bool _isDiscovering;
    
    [ObservableProperty]
    private ObservableCollection<DeviceItemViewModel> _devices = new();
    
    [ObservableProperty]
    private DeviceItemViewModel? _selectedDevice;

    public MainViewModel(ITapoApiClient tapoClient)
    {
        _tapoClient = tapoClient;
        Title = "Tapo Device Manager";
        
        // Load saved credentials if any
        LoadCredentials();
    }
    
    [RelayCommand(CanExecute = nameof(CanDiscoverDevices))]
    private async Task DiscoverDevicesAsync()
    {
        if (IsDiscovering)
            return;
        
        try
        {
            IsDiscovering = true;
            ClearError();
            Devices.Clear();
            
            // Discovery doesn't require credentials - it's just network scanning
            // Credentials are only needed for device control after discovery
            
            // Save credentials if provided
            if (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password))
            {
                _tapoClient.SetCredentials(Username, Password);
                SaveCredentials();
            }
            
            // Auto-discovery: scan all local networks if no target specified
            var networkToScan = string.IsNullOrWhiteSpace(NetworkTarget) ? "" : NetworkTarget;
            
            System.Diagnostics.Debug.WriteLine($"=== STARTING DISCOVERY ===");
            System.Diagnostics.Debug.WriteLine($"Network target: '{networkToScan}' (empty = scan all networks)");
            
            var discoveredDevices = await _tapoClient.DiscoverDevicesAsync(networkToScan, 20); // Increased timeout for comprehensive discovery
            
            System.Diagnostics.Debug.WriteLine($"=== DISCOVERY RESULTS ===");
            System.Diagnostics.Debug.WriteLine($"Found {discoveredDevices.Count()} devices total");
            
            foreach (var device in discoveredDevices)
            {
                System.Diagnostics.Debug.WriteLine($"Device: {device.IpAddress} - {device.DeviceInfo.Model}");
                var deviceViewModel = new DeviceItemViewModel(_tapoClient, device);
                Devices.Add(deviceViewModel);
            }
            
            if (!Devices.Any())
            {
                ShowError("No Tapo devices discovered on any network. Try running NetworkDiscovery tool in Tools/ folder for detailed network analysis.");
            }
            else
            {
                var credentialNote = string.IsNullOrWhiteSpace(Username) 
                    ? " (Create local camera account in Tapo mobile app to control devices)" 
                    : "";
                ShowError($"✅ Found {Devices.Count} Tapo device(s) across all networks{credentialNote}");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Discovery failed: {ex.Message}");
        }
        finally
        {
            IsDiscovering = false;
        }
    }
    
    private bool CanDiscoverDevices()
    {
        return !IsDiscovering;
    }
    
    [RelayCommand]
    private async Task RefreshDevicesAsync()
    {
        var refreshTasks = Devices.Select(d => d.RefreshDataCommand.ExecuteAsync(null));
        await Task.WhenAll(refreshTasks);
    }
    
    [RelayCommand]
    private void ClearDevices()
    {
        Devices.Clear();
        SelectedDevice = null;
    }
    
    [RelayCommand]
    private void ClearCredentials()
    {
        Username = string.Empty;
        Password = string.Empty;
        NetworkTarget = string.Empty;
        
        // Clear from storage as well
        try
        {
            Preferences.Remove("tapo_username");
            Preferences.Remove("tapo_password");
            Preferences.Remove("tapo_network");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing preferences: {ex.Message}");
        }
    }
    
    private void LoadCredentials()
    {
        try
        {
            // Clear any old stored credentials and network settings to prevent auto-restore
            Preferences.Remove("tapo_username");
            Preferences.Remove("tapo_password");
            Preferences.Remove("tapo_network");
            
            // Always start with empty network target for full auto-discovery
            NetworkTarget = "";
            
            System.Diagnostics.Debug.WriteLine("All stored preferences cleared, starting fresh with auto-discovery");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error clearing preferences: {ex.Message}");
            // Ensure network target is empty even if clearing fails
            NetworkTarget = "";
        }
    }
    
    private void SaveCredentials()
    {
        try
        {
            Preferences.Set("tapo_username", Username);
            Preferences.Set("tapo_password", Password);
            Preferences.Set("tapo_network", NetworkTarget);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving credentials: {ex.Message}");
        }
    }
}