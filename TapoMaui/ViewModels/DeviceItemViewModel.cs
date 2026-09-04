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
    
    [ObservableProperty]
    private bool _isStreaming;
    
    [ObservableProperty]
    private bool _isStreamLoading;
    
    [ObservableProperty]
    private string _streamUrl = string.Empty;
    
    [ObservableProperty] 
    private bool _showVideoPlayer;

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
    
    public bool IsCamera => Device.DeviceType == DeviceType.CameraPtz || 
                           Device.DeviceType == DeviceType.Camera ||
                           Device.DeviceInfo.Model.Contains("Camera", StringComparison.OrdinalIgnoreCase);
    
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
    
    [RelayCommand]
    private async Task StartStreamAsync()
    {
        if (!IsCamera || IsStreamLoading || IsStreaming) return;
        
        try
        {
            IsStreamLoading = true;
            ClearError();
            
            System.Diagnostics.Debug.WriteLine($"🔄 Starting stream process for {Device.IpAddress}...");
            
            // Check if we have credentials for streaming
            var username = _tapoClient.GetUsername();
            var password = _tapoClient.GetPassword();
            
            System.Diagnostics.Debug.WriteLine($"📋 Credentials check:");
            System.Diagnostics.Debug.WriteLine($"   Username: '{username}' (length: {username.Length})");
            System.Diagnostics.Debug.WriteLine($"   Password: '{(string.IsNullOrEmpty(password) ? "EMPTY" : "***")}' (length: {password.Length})");
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("❌ Camera credentials required. Enter username/password in main screen first!");
                return;
            }
            
            var ip = Device.IpAddress;
            System.Diagnostics.Debug.WriteLine($"🌐 Target IP: {ip}");
            
            // Add small delay to show loading state
            await Task.Delay(1000);
            
            // Enhanced network connectivity testing for Tapo cameras
            System.Diagnostics.Debug.WriteLine($"🔍 Testing network connectivity to {ip}...");
            
            bool isReachable = false;
            string connectivityStatus = "";
            
            try
            {
                // Test HTTPS with SSL certificate bypass (Tapo cameras use self-signed certs)
                using var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                
                using var client = new HttpClient(handler);
                client.Timeout = TimeSpan.FromSeconds(8);
                
                var testUrl = $"https://{ip}";
                System.Diagnostics.Debug.WriteLine($"🌐 Testing HTTPS (SSL bypass): {testUrl}");
                
                var response = await client.GetAsync(testUrl);
                isReachable = true;
                connectivityStatus = $"HTTPS SSL-bypass successful ({response.StatusCode})";
                System.Diagnostics.Debug.WriteLine($"✅ {connectivityStatus}");
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("SSL") || httpEx.Message.Contains("certificate"))
            {
                System.Diagnostics.Debug.WriteLine($"🔐 SSL certificate error (normal for Tapo): {httpEx.Message}");
                connectivityStatus = "SSL certificate issues (normal for Tapo cameras)";
                
                // SSL issues are normal for Tapo cameras, try HTTP fallback
                try
                {
                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var testUrl = $"http://{ip}";
                    System.Diagnostics.Debug.WriteLine($"🌐 Testing HTTP fallback: {testUrl}");
                    
                    var response = await client.GetAsync(testUrl);
                    isReachable = true;
                    connectivityStatus += $" + HTTP fallback OK ({response.StatusCode})";
                    System.Diagnostics.Debug.WriteLine($"✅ HTTP fallback successful ({response.StatusCode})");
                }
                catch (Exception httpFallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ HTTP fallback also failed: {httpFallbackEx.Message}");
                    connectivityStatus += $" + HTTP fallback failed";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Connection test failed: {ex.Message}");
                connectivityStatus = $"Connection failed: {ex.Message}";
            }
            
            if (!isReachable)
            {
                ShowError($"❌ Cannot reach camera at {ip}. Network issue or incorrect IP. Status: {connectivityStatus}");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"✅ Basic connectivity confirmed: {connectivityStatus}");
            
            // Test RTSP port connectivity
            System.Diagnostics.Debug.WriteLine($"🔍 Testing RTSP port 554 on {ip}...");
            bool rtspPortOpen = await TestRTSPConnectivity(ip, 554);
            
            if (!rtspPortOpen)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ RTSP port 554 not accessible on {ip}");
                ShowError($"⚠️ RTSP port 554 not accessible on {ip}. Camera might not support RTSP or it's disabled.");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"✅ RTSP port 554 is accessible on {ip}");
            }
            
            // Check camera RTSP configuration and provide detailed guidance
            await ProvideRTSPTroubleshootingGuidance(ip, username, password, rtspPortOpen);
            
            // Generate RTSP URLs with proper Tapo format
            var streamUrls = new[]
            {
                $"rtsp://{username}:{password}@{ip}:554/stream1",           // High quality (1080p)
                $"rtsp://{username}:{password}@{ip}:554/stream2",           // Standard quality (720p)  
                $"rtsp://{username}:{password}@{ip}/stream1",               // Without explicit port
                $"rtsp://{username}:{password}@{ip}/stream2",               // Without port (lower quality)
                $"rtsp://{username}:{password}@{ip}:554/live/ch00_0",       // Alternative format
                $"rtsp://{username}:{password}@{ip}:554/live/ch00_1",       // Alternative format (lower)
                $"rtsp://{username}:{password}@{ip}:8554/stream1",          // Alternative RTSP port
                $"rtsp://{username}:{password}@{ip}:8554/stream2"           // Alternative port + quality
            };
            
            System.Diagnostics.Debug.WriteLine($"📹 Generated {streamUrls.Length} RTSP URL formats following TP-Link standard");
            
            // Use the standard Tapo format (most reliable)
            var selectedUrl = streamUrls[0]; // rtsp://username:password@IP:554/stream1
            
            // Set stream properties
            StreamUrl = selectedUrl;
            IsStreaming = true;
            ShowVideoPlayer = true;
            
            System.Diagnostics.Debug.WriteLine($"✅ Stream URL set: rtsp://{username}:***@{ip}:554/stream1");
            
            // Provide appropriate status message based on test results
            string statusMessage;
            if (rtspPortOpen)
            {
                statusMessage = $"🔴 RTSP stream ready for {ip}! MediaElement may have limited RTSP support - use 'Open VLC' for best results.";
            }
            else
            {
                statusMessage = $"⚠️ RTSP port 554 not accessible on {ip}. Stream URL created but streaming will likely fail. Check camera RTSP settings in Tapo mobile app.";
            }
            
            ShowError(statusMessage);
            
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Stream start error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"📋 Stack trace: {ex.StackTrace}");
            ShowError($"❌ Stream error: {ex.Message}");
            IsStreaming = false;
            ShowVideoPlayer = false;
            StreamUrl = string.Empty;
        }
        finally
        {
            IsStreamLoading = false;
        }
    }
    
    private async Task<bool> TestRTSPConnectivity(string ip, int port)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔍 Testing TCP connection to {ip}:{port}...");
            
            using var client = new System.Net.Sockets.TcpClient();
            var connectTask = client.ConnectAsync(ip, port);
            var timeoutTask = Task.Delay(8000); // 8 seconds for Tapo cameras (they can be slow)
            
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            
            if (completedTask == connectTask && client.Connected)
            {
                System.Diagnostics.Debug.WriteLine($"✅ TCP connection successful to {ip}:{port}");
                
                // For RTSP port 554, verify RTSP protocol is working
                if (port == 554)
                {
                    try
                    {
                        var stream = client.GetStream();
                        
                        // Send RTSP OPTIONS request to verify RTSP server
                        var rtspRequest = $"OPTIONS rtsp://{ip}:{port}/stream1 RTSP/1.0\r\nCSeq: 1\r\nUser-Agent: TapoMaui/1.0\r\n\r\n";
                        var requestBytes = System.Text.Encoding.ASCII.GetBytes(rtspRequest);
                        
                        await stream.WriteAsync(requestBytes, 0, requestBytes.Length);
                        System.Diagnostics.Debug.WriteLine($"📤 Sent RTSP OPTIONS request to {ip}:{port}");
                        
                        var buffer = new byte[1024];
                        var readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                        var timeoutReadTask = Task.Delay(5000);
                        
                        var readCompleted = await Task.WhenAny(readTask, timeoutReadTask);
                        
                        if (readCompleted == readTask && readTask.Result > 0)
                        {
                            var response = System.Text.Encoding.ASCII.GetString(buffer, 0, readTask.Result);
                            System.Diagnostics.Debug.WriteLine($"📥 RTSP Response: {response.Substring(0, Math.Min(response.Length, 200))}...");
                            
                            if (response.Contains("RTSP/1.0") || response.Contains("200 OK"))
                            {
                                System.Diagnostics.Debug.WriteLine($"✅ RTSP server confirmed - camera supports streaming");
                                return true;
                            }
                            else if (response.Contains("401") || response.Contains("Unauthorized"))
                            {
                                System.Diagnostics.Debug.WriteLine($"🔐 RTSP server active but requires authentication");
                                return true; // Port is open, just needs credentials
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ Unexpected RTSP response format");
                                return true; // Port is open, might still work
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"⏰ No RTSP response received within 5 seconds");
                            return false; // Port open but no RTSP protocol
                        }
                    }
                    catch (Exception rtspEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ RTSP protocol test failed: {rtspEx.Message}");
                        return true; // TCP connection worked, might be RTSP version issue
                    }
                }
                
                return true; // Non-RTSP port, TCP connection is enough
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ TCP connection timeout to {ip}:{port} (8 seconds)");
                
                // Provide specific guidance for blocked ports
                if (port == 554)
                {
                    System.Diagnostics.Debug.WriteLine($"💡 Port 554 blocked - common causes:");
                    System.Diagnostics.Debug.WriteLine($"   • RTSP disabled in Tapo app camera settings");
                    System.Diagnostics.Debug.WriteLine($"   • Windows Firewall blocking outbound connections");
                    System.Diagnostics.Debug.WriteLine($"   • Router firewall restrictions");
                    System.Diagnostics.Debug.WriteLine($"   • Camera firmware doesn't support RTSP (older models)");
                }
                
                return false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ TCP connection failed to {ip}:{port}: {ex.Message}");
            
            // Provide specific error guidance
            if (ex.Message.Contains("No connection could be made"))
            {
                System.Diagnostics.Debug.WriteLine($"💡 Connection refused - port likely closed or service not running");
            }
            else if (ex.Message.Contains("timed out"))
            {
                System.Diagnostics.Debug.WriteLine($"💡 Connection timed out - firewall or network issue");
            }
            
            return false;
        }
    }
    
    [RelayCommand]
    private void StopStream()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🛑 Stopping camera stream: {Device.IpAddress}");
            
            IsStreaming = false;
            ShowVideoPlayer = false;
            StreamUrl = string.Empty;
            IsStreamLoading = false;
            
            ShowError($"⚫ Stream stopped for {Device.IpAddress}");
            
            System.Diagnostics.Debug.WriteLine($"✅ Stream stopped successfully");
        }
        catch (Exception ex)
        {
            ShowError($"Error stopping stream: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ Stop stream error: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void ToggleVideoPlayer()
    {
        if (IsStreamLoading) return; // Prevent multiple clicks during loading
        
        if (IsStreaming)
        {
            StopStream();
        }
        else
        {
            _ = StartStreamAsync();
        }
    }
    
    [RelayCommand]
    private async Task OpenVideoPlayerAsync()
    {
        try
        {
            if (!IsCamera)
            {
                ShowError("This device is not a camera");
                return;
            }
            
            // Navigate to full-screen video player using the modern approach
            var videoPlayerPage = new Views.VideoPlayerPage(this);
            var mainPage = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (mainPage != null)
            {
                await mainPage.Navigation.PushAsync(videoPlayerPage);
            }
            else
            {
                ShowError("Unable to navigate to video player");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error opening video player: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (IsStreamLoading || IsBusy) return;
        
        try
        {
            IsStreamLoading = true;
            ClearError();
            
            var ip = Device.IpAddress;
            System.Diagnostics.Debug.WriteLine($"🧪 COMPREHENSIVE TAPO CAMERA DIAGNOSIS: {ip}");
            
            ShowError($"🧪 Testing Tapo camera at {ip}...");
            await Task.Delay(500);
            
            // Test 1: HTTPS with SSL bypass (Tapo cameras use self-signed certificates)
            bool httpsWorking = false;
            try
            {
                using var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                
                using var client = new HttpClient(handler);
                client.Timeout = TimeSpan.FromSeconds(8);
                var response = await client.GetAsync($"https://{ip}");
                httpsWorking = true;
                System.Diagnostics.Debug.WriteLine($"✅ HTTPS (SSL bypass) test passed ({response.StatusCode})");
                ShowError($"✅ HTTPS: OK ({response.StatusCode}) - Camera accessible");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ HTTPS test failed: {ex.Message}");
                if (ex.Message.Contains("SSL") || ex.Message.Contains("certificate"))
                {
                    ShowError($"🔐 SSL Error - This is normal for Tapo cameras with self-signed certificates");
                }
                else
                {
                    ShowError($"❌ HTTPS: Failed - {ex.Message.Split('.')[0]}");
                }
            }
            
            await Task.Delay(500);
            
            // Test 2: RTSP port connectivity (port 554)
            bool rtspWorking = await TestRTSPConnectivity(ip, 554);
            if (rtspWorking)
            {
                System.Diagnostics.Debug.WriteLine($"✅ RTSP port 554 accessible");
                ShowError($"✅ RTSP Port 554: OPEN - Ready for streaming");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ RTSP port 554 not accessible");
                ShowError($"❌ RTSP Port 554: BLOCKED - Check camera RTSP settings");
            }
            
            await Task.Delay(500);
            
            // Test 3: ONVIF management port (port 2020)
            bool onvifWorking = await TestRTSPConnectivity(ip, 2020);
            if (onvifWorking)
            {
                System.Diagnostics.Debug.WriteLine($"✅ ONVIF port 2020 accessible");
                ShowError($"✅ ONVIF Port 2020: OPEN - Management interface available");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"❌ ONVIF port 2020 not accessible");
                ShowError($"⚠️ ONVIF Port 2020: CLOSED - PTZ control may not work");
            }
            
            await Task.Delay(500);
            
            // Test 4: Credentials validation
            var username = _tapoClient.GetUsername();
            var password = _tapoClient.GetPassword();
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                System.Diagnostics.Debug.WriteLine($"❌ Camera credentials missing");
                ShowError($"❌ Credentials: MISSING - Create local account in Tapo app first");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"✅ Credentials configured: user={username}");
                ShowError($"✅ Credentials: OK (username: {username})");
            }
            
            await Task.Delay(1000);
            
            // Final diagnosis and recommendations
            var diagnosis = new List<string>();
            var recommendations = new List<string>();
            
            if (!rtspWorking)
            {
                diagnosis.Add("RTSP Port 554 blocked");
                recommendations.Add("📱 Enable RTSP in Tapo mobile app: Camera Settings → Advanced → Camera Account");
                recommendations.Add("🔥 Check Windows Firewall - allow port 554");
                recommendations.Add("📶 Verify camera and device on same network");
            }
            
            if (!httpsWorking)
            {
                diagnosis.Add("HTTPS connectivity issues");
                recommendations.Add("🌐 Check network connectivity between devices");
                recommendations.Add("⚡ Try power cycling the camera");
            }
            
            if (string.IsNullOrEmpty(username))
            {
                diagnosis.Add("No local camera account");
                recommendations.Add("📱 Open Tapo app → Camera → Settings → Advanced → Camera Account → Create New");
                recommendations.Add("⚠️ Use LOCAL account, not TP-Link cloud credentials");
            }
            
            // Summary
            if (httpsWorking && rtspWorking && !string.IsNullOrEmpty(username))
            {
                ShowError($"🎯 ALL TESTS PASSED! Camera should stream successfully via RTSP.");
            }
            else
            {
                var issueCount = diagnosis.Count;
                ShowError($"⚠️ {issueCount} issue(s) found: {string.Join(", ", diagnosis)}");
                
                System.Diagnostics.Debug.WriteLine($"\n📋 TROUBLESHOOTING RECOMMENDATIONS:");
                foreach (var rec in recommendations)
                {
                    System.Diagnostics.Debug.WriteLine($"   {rec}");
                }
            }
            
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Connection test error: {ex.Message}");
            ShowError($"❌ Test failed: {ex.Message}");
        }
        finally
        {
            IsStreamLoading = false;
        }
    }
    
    private async Task CheckCameraRTSPConfig(string ip, string username, string password)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔧 Checking camera RTSP configuration at {ip}...");
            
            // Try to access camera settings via HTTPS API
            using var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            
            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(8);
            
            // Add basic authentication if credentials are provided
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var authValue = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
            }
            
            try
            {
                // Try HTTPS first (modern Tapo cameras)
                var httpsResponse = await client.GetAsync($"https://{ip}/");
                System.Diagnostics.Debug.WriteLine($"🔐 HTTPS connection: {httpsResponse.StatusCode}");
                
                if (httpsResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    System.Diagnostics.Debug.WriteLine($"🔑 Camera requires authentication (401 Unauthorized)");
                }
                else if (httpsResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Camera HTTPS accessible (404 is normal for root path)");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Camera HTTPS accessible ({httpsResponse.StatusCode})");
                }
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("SSL"))
            {
                System.Diagnostics.Debug.WriteLine($"🚫 SSL connection failed: {httpEx.Message}");
                System.Diagnostics.Debug.WriteLine($"💡 This is common with Tapo cameras - they use self-signed certificates");
            }
            catch (Exception httpsEx)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ HTTPS test failed: {httpsEx.Message}");
            }
            
            // Check for common ONVIF/RTSP management ports
            var managementPorts = new[] { 2020, 8080, 80, 8000 };
            
            foreach (var port in managementPorts)
            {
                try
                {
                    var portResponse = await client.GetAsync($"http://{ip}:{port}/");
                    if (portResponse.IsSuccessStatusCode || portResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Management interface found on port {port} ({portResponse.StatusCode})");
                        break;
                    }
                }
                catch
                {
                    // Port not accessible, continue
                }
            }
            
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Camera config check failed: {ex.Message}");
        }
        
        // Provide specific troubleshooting guidance
        System.Diagnostics.Debug.WriteLine($"");
        System.Diagnostics.Debug.WriteLine($"📋 RTSP TROUBLESHOOTING GUIDE:");
        System.Diagnostics.Debug.WriteLine($"   1. Open Tapo mobile app → Select your camera");
        System.Diagnostics.Debug.WriteLine($"   2. Go to Settings (gear icon) → Advanced Settings");
        System.Diagnostics.Debug.WriteLine($"   3. Look for 'Camera Account' or 'Local Account'");
        System.Diagnostics.Debug.WriteLine($"   4. Create username/password if not exists");
        System.Diagnostics.Debug.WriteLine($"   5. Ensure RTSP/ONVIF is enabled (some models need this turned on)");
        System.Diagnostics.Debug.WriteLine($"   6. Check if port 554 is open in router/firewall");
        System.Diagnostics.Debug.WriteLine($"   Current credentials: username='{username}', password={new string('*', password.Length)}");
    }
    
    private async Task ProvideRTSPTroubleshootingGuidance(string ip, string username, string password, bool rtspPortOpen)
    {
        System.Diagnostics.Debug.WriteLine($"");
        System.Diagnostics.Debug.WriteLine($"🔧 TAPO CAMERA RTSP TROUBLESHOOTING GUIDE");
        System.Diagnostics.Debug.WriteLine($"Camera: {ip}, User: {username}, RTSP Port: {(rtspPortOpen ? "OPEN" : "BLOCKED")}");
        
        if (!rtspPortOpen)
        {
            System.Diagnostics.Debug.WriteLine($"");
            System.Diagnostics.Debug.WriteLine($"❌ RTSP PORT 554 BLOCKED - SOLUTIONS:");
            System.Diagnostics.Debug.WriteLine($"");
            System.Diagnostics.Debug.WriteLine($"📱 1. ENABLE RTSP IN TAPO MOBILE APP:");
            System.Diagnostics.Debug.WriteLine($"   • Open Tapo app on your phone");
            System.Diagnostics.Debug.WriteLine($"   • Tap your camera to view live feed");
            System.Diagnostics.Debug.WriteLine($"   • Tap Settings (gear icon) in top-right");
            System.Diagnostics.Debug.WriteLine($"   • Go to Advanced Settings");
            System.Diagnostics.Debug.WriteLine($"   • Look for 'Camera Account' or 'Local Account'");
            System.Diagnostics.Debug.WriteLine($"   • Create username/password for local access");
            System.Diagnostics.Debug.WriteLine($"   • Some models have 'RTSP' or 'Third-party Access' toggle - enable it");
            System.Diagnostics.Debug.WriteLine($"");
            System.Diagnostics.Debug.WriteLine($"🔥 2. CHECK WINDOWS FIREWALL:");
            System.Diagnostics.Debug.WriteLine($"   • Windows Security → Firewall & Network Protection");
            System.Diagnostics.Debug.WriteLine($"   • Allow an app through firewall");
            System.Diagnostics.Debug.WriteLine($"   • Add TapoMaui.exe to allowed apps");
            System.Diagnostics.Debug.WriteLine($"   • Or temporarily disable firewall for testing");
            System.Diagnostics.Debug.WriteLine($"");
            System.Diagnostics.Debug.WriteLine($"📶 3. NETWORK TROUBLESHOOTING:");
            System.Diagnostics.Debug.WriteLine($"   • Ensure camera and PC are on same network/VLAN");
            System.Diagnostics.Debug.WriteLine($"   • Check router firewall settings");
            System.Diagnostics.Debug.WriteLine($"   • Try connecting PC to same WiFi as camera");
            System.Diagnostics.Debug.WriteLine($"   • Power cycle camera (unplug 10 seconds)");
            System.Diagnostics.Debug.WriteLine($"");
            System.Diagnostics.Debug.WriteLine($"🛠️ 4. CAMERA FIRMWARE:");
            System.Diagnostics.Debug.WriteLine($"   • Update camera firmware via Tapo app");
            System.Diagnostics.Debug.WriteLine($"   • Some older models don't support RTSP");
            System.Diagnostics.Debug.WriteLine($"   • Check model compatibility at tp-link.com/support");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"✅ RTSP port is accessible - camera should stream successfully!");
        }
        
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            System.Diagnostics.Debug.WriteLine($"");
            System.Diagnostics.Debug.WriteLine($"❌ MISSING CAMERA CREDENTIALS:");
            System.Diagnostics.Debug.WriteLine($"   • Tapo cameras DO NOT have default passwords");
            System.Diagnostics.Debug.WriteLine($"   • You MUST create a local account in Tapo mobile app");
            System.Diagnostics.Debug.WriteLine($"   • This is NOT your TP-Link cloud account");
            System.Diagnostics.Debug.WriteLine($"   • Go to Camera Settings → Advanced → Camera Account");
        }
        
        System.Diagnostics.Debug.WriteLine($"");
        System.Diagnostics.Debug.WriteLine($"🎯 QUICK TEST STEPS:");
        System.Diagnostics.Debug.WriteLine($"   1. Use 'Test' button to diagnose specific issues");
        System.Diagnostics.Debug.WriteLine($"   2. Try VLC Media Player with RTSP URL if MediaElement fails");
        System.Diagnostics.Debug.WriteLine($"   3. Use 'Open VLC' button to copy URL and test externally");
        System.Diagnostics.Debug.WriteLine($"");
        
        // Give user time to see the guidance
        await Task.Delay(100);
    }
    
    // Helper method to get password (for URL masking in UI)
    public string GetPassword() => _tapoClient.GetPassword();
    
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