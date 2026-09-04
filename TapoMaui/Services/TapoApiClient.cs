using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TapoMaui.Models;
using DeviceInfo = TapoMaui.Models.DeviceInfo;
using DeviceType = TapoMaui.Models.DeviceType;
using Color = TapoMaui.Models.Color;

namespace TapoMaui.Services;

public class TapoApiClient : ITapoApiClient
{
    private readonly HttpClient _httpClient;
    private string _username;
    private string _password;

    public TapoApiClient(HttpClient httpClient, string username = "", string password = "")
    {
        _httpClient = httpClient;
        _username = username;
        _password = password;
        
        // Configure HttpClient
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public void SetCredentials(string username, string password)
    {
        // IMPORTANT: Tapo cameras do NOT have default credentials!
        // Users must create a local "Camera Account" in the Tapo mobile app first:
        // 1. Open Tapo app -> Select camera -> Settings (gear icon)
        // 2. Advanced Settings -> Camera Account  
        // 3. Create custom username/password for local access
        // These are NOT the same as TP-Link cloud account credentials!
        
        _username = username ?? string.Empty;
        _password = password ?? string.Empty;
        System.Diagnostics.Debug.WriteLine($"Local camera credentials set for: {username}");
    }

    public async Task<IEnumerable<DiscoveredDevice>> DiscoverDevicesAsync(string targetNetwork = "", int timeoutSeconds = 10)
    {
        var devices = new List<DiscoveredDevice>();
        
        try
        {
            System.Diagnostics.Debug.WriteLine($"=== ENHANCED TAPO DISCOVERY STARTING ===");
            System.Diagnostics.Debug.WriteLine($"Target network: '{targetNetwork}' (empty = auto-scan all networks)");
            
            // Auto-detect network ranges if not provided  
            var networkRanges = string.IsNullOrWhiteSpace(targetNetwork) 
                ? GetLocalNetworkRanges() 
                : new[] { targetNetwork };
            
            System.Diagnostics.Debug.WriteLine($"Network ranges to scan: {string.Join(", ", networkRanges)}");
            System.Diagnostics.Debug.WriteLine($"Using comprehensive discovery like NetworkDiscovery tool...");
            
            // Enhanced discovery using ALL methods from NetworkDiscovery tool
            var discoveryTasks = new List<Task>
            {
                DiscoverTapoDevicesUDP(devices, 9999, timeoutSeconds),    // Smart plugs/bulbs
                DiscoverTapoDevicesUDP(devices, 20002, timeoutSeconds),   // Cameras
                DiscoverTapoDevicesMDNS(devices, timeoutSeconds),         // mDNS discovery
                DiscoverTapoDevicesTCP(devices, networkRanges, timeoutSeconds), // TCP/HTTP discovery 
                SendTapoDiscoveryProbes(networkRanges, timeoutSeconds)    // Activate dormant APIs
            };
            
            // Add comprehensive network scanning for each detected range
            foreach (var network in networkRanges)
            {
                discoveryTasks.Add(ScanNetworkForTapoDevices(devices, network, timeoutSeconds));
            }
            
            System.Diagnostics.Debug.WriteLine($"🚀 Starting {discoveryTasks.Count} discovery tasks concurrently...");
            await Task.WhenAll(discoveryTasks);
            
            var uniqueDevices = devices.GroupBy(d => d.IpAddress).Select(g => g.First()).ToList();
            System.Diagnostics.Debug.WriteLine($"=== DISCOVERY COMPLETED ===");
            System.Diagnostics.Debug.WriteLine($"Total detections: {devices.Count}, Unique devices: {uniqueDevices.Count}");
            
            // Log found devices for debugging
            foreach (var device in uniqueDevices)
            {
                System.Diagnostics.Debug.WriteLine($"✅ DISCOVERED: {device.IpAddress} - {device.DeviceInfo.Model} ({device.DeviceType})");
            }
            
            return uniqueDevices;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Discovery error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        return devices.GroupBy(d => d.IpAddress).Select(g => g.First()).ToList(); // Remove duplicates
    }
    
    private string[] GetLocalNetworkRanges()
    {
        var networks = new List<string>();
        
        try
        {
            System.Diagnostics.Debug.WriteLine("=== AUTO-DETECTING NETWORK INTERFACES ===");
            
            // Use the same logic as NetworkDiscovery tool
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                             ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
                             ni.OperationalStatus == OperationalStatus.Up);

            foreach (var ni in networkInterfaces)
            {
                System.Diagnostics.Debug.WriteLine($"Interface: {ni.Name} ({ni.NetworkInterfaceType}) - {ni.OperationalStatus}");
                
                var properties = ni.GetIPProperties();
                foreach (var ip in properties.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(ip.Address))
                    {
                        var networkBase = GetNetworkBase(ip.Address);
                        if (networkBase != null)
                        {
                            networks.Add($"{networkBase}.255");
                            System.Diagnostics.Debug.WriteLine($"✅ Detected network: {ip.Address} -> {networkBase}.255");
                        }
                    }
                }
            }
            
            var uniqueNetworks = networks.Distinct().ToArray();
            System.Diagnostics.Debug.WriteLine($"Networks to scan: {string.Join(", ", uniqueNetworks)}");
            
            // Comprehensive fallbacks if no networks detected
            if (uniqueNetworks.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine("No networks auto-detected, using comprehensive fallbacks");
                uniqueNetworks = new[] { 
                    "192.168.1.255", "192.168.7.255", "192.168.0.255", "192.168.2.255", 
                    "10.0.0.255", "10.0.1.255", "172.16.0.255" 
                };
            }
            
            return uniqueNetworks;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error detecting network ranges: {ex.Message}");
            // Return comprehensive fallback ranges
            return new[] { 
                "192.168.1.255", "192.168.7.255", "192.168.0.255", "192.168.2.255",
                "10.0.0.255", "10.0.1.255", "172.16.0.255"
            };
        }
    }

    private static string? GetNetworkBase(IPAddress ip)
    {
        var octets = ip.ToString().Split('.');
        if (octets.Length == 4)
        {
            return $"{octets[0]}.{octets[1]}.{octets[2]}";
        }
        return null;
    }

    private async Task SendTapoDiscoveryProbes(string[] networkRanges, int timeoutSeconds)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("Sending Tapo discovery probes to activate local APIs...");
            
            using var client = new UdpClient();
            client.EnableBroadcast = true;
            
            // Discovery probe packet as mentioned in the GitHub issue
            // This activates the local API on devices that need it
            var discoveryProbe = Encoding.UTF8.GetBytes("{\"system\":{\"get_sysinfo\":{}}}");
            
            foreach (var networkRange in networkRanges)
            {
                var networkBase = networkRange.Replace(".255", "");
                
                // Send probes to common Tapo device IP addresses in the subnet
                var commonLastOctets = new[] { 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 200, 201, 202, 203, 204, 205 };
                
                foreach (var lastOctet in commonLastOctets)
                {
                    var targetIP = $"{networkBase}.{lastOctet}";
                    
                    try
                    {
                        // Send probe to UDP port 9999 (plugs) and 20002 (cameras)
                        await client.SendAsync(discoveryProbe, new IPEndPoint(IPAddress.Parse(targetIP), 9999));
                        await client.SendAsync(discoveryProbe, new IPEndPoint(IPAddress.Parse(targetIP), 20002));
                    }
                    catch
                    {
                        // Ignore failures - target device might not exist
                    }
                }
            }
            
            // Also send broadcast probes for each network
            foreach (var networkRange in networkRanges)
            {
                var broadcastIP = IPAddress.Parse(networkRange.EndsWith(".255") ? networkRange : $"{networkRange}.255");
                await client.SendAsync(discoveryProbe, new IPEndPoint(broadcastIP, 9999));
                await client.SendAsync(discoveryProbe, new IPEndPoint(broadcastIP, 20002));
            }
            
            System.Diagnostics.Debug.WriteLine("Discovery probes sent - this may activate dormant Tapo device APIs");
            
            // Wait a moment for devices to activate their APIs
            await Task.Delay(2000);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Discovery probe sending failed: {ex.Message}");
        }
    }

    private async Task DiscoverTapoDevicesTCP(List<DiscoveredDevice> devices, string[] networkRanges, int timeoutSeconds)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("=== TCP/HTTP DISCOVERY ===");
            
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(40); // Limit concurrent HTTP requests
            
            foreach (var networkRange in networkRanges)
            {
                var baseIp = networkRange.Replace(".255", "");
                System.Diagnostics.Debug.WriteLine($"TCP Discovery: Scanning {baseIp}.1-254...");
                
                for (int i = 1; i <= 254; i++)
                {
                    var ip = $"{baseIp}.{i}";
                    tasks.Add(CheckTapoDeviceHTTP(ip, devices, semaphore));
                }
            }
            
            await Task.WhenAll(tasks);
            System.Diagnostics.Debug.WriteLine("TCP/HTTP discovery completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TCP discovery error: {ex.Message}");
        }
    }

    private async Task CheckTapoDeviceHTTP(string ipAddress, List<DiscoveredDevice> devices, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        
        try
        {
            // Check for multiple Tapo-specific ports (like NetworkDiscovery tool does)
            var tapoIndicators = new[]
            {
                (443, "HTTPS"),    // Modern Tapo cameras use HTTPS
                (2020, "ONVIF"),   // ONVIF protocol for cameras
                (8800, "TP-Link"), // TP-Link proprietary streaming
                (554, "RTSP")      // RTSP streaming
            };

            var openPorts = new List<(int port, string protocol)>();
            
            // Check all Tapo-specific ports
            foreach (var (port, protocol) in tapoIndicators)
            {
                if (await IsPortOpen(ipAddress, port, 300))
                {
                    openPorts.Add((port, protocol));
                    System.Diagnostics.Debug.WriteLine($"🔍 Found {protocol} port {port} on {ipAddress}");
                }
            }

            // NetworkDiscovery logic: need 2+ Tapo ports to consider it a Tapo device
            if (openPorts.Count >= 2)
            {
                var isTapoDevice = false;
                string deviceType = "Tapo Device";
                string discoveryMethod = "Multi-Port Detection";
                
                // Try ONVIF discovery first if port 2020 is open
                if (openPorts.Any(p => p.port == 2020))
                {
                    if (await CheckONVIFCapabilities(ipAddress))
                    {
                        isTapoDevice = true;
                        deviceType = "Tapo Camera (ONVIF)";
                        discoveryMethod = "ONVIF Detection";
                        System.Diagnostics.Debug.WriteLine($"✅ ONVIF TAPO CAMERA: {ipAddress}");
                    }
                }
                
                // Try HTTPS discovery if port 443 is open
                if (!isTapoDevice && openPorts.Any(p => p.port == 443))
                {
                    if (await CheckTapoHTTPS(ipAddress))
                    {
                        isTapoDevice = true;
                        deviceType = "Tapo Camera (HTTPS)";
                        discoveryMethod = "HTTPS Detection";
                        System.Diagnostics.Debug.WriteLine($"✅ HTTPS TAPO CAMERA: {ipAddress}");
                    }
                }
                
                // If ports 443 and 8800 are open, it's very likely a Tapo camera
                if (!isTapoDevice && openPorts.Any(p => p.port == 443) && openPorts.Any(p => p.port == 8800))
                {
                    isTapoDevice = true;
                    deviceType = "Tapo Camera with Streaming";
                    discoveryMethod = "HTTPS + Streaming Ports";
                    System.Diagnostics.Debug.WriteLine($"✅ TAPO CAMERA (HTTPS+STREAMING): {ipAddress}");
                }

                if (isTapoDevice)
                {
                    var deviceInfo = new DeviceInfo
                    {
                        IpAddress = ipAddress,
                        Model = "Tapo Camera",
                        DeviceOn = false
                    };
                    
                    lock (devices)
                    {
                        if (!devices.Any(d => d.IpAddress == ipAddress))
                        {
                            devices.Add(new DiscoveredDevice
                            {
                                DeviceInfo = deviceInfo,
                                DeviceType = DeviceType.CameraPtz,
                                IpAddress = ipAddress
                            });
                            System.Diagnostics.Debug.WriteLine($"🎯 TCP ADDED: {ipAddress} - {deviceType} via {discoveryMethod}");
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore individual device errors during TCP scanning
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<bool> CheckTapoHTTPS(string ipAddress)
    {
        try
        {
            using var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            
            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(3);
            
            // Try to connect to HTTPS port - Tapo cameras respond differently than generic HTTPS servers
            var response = await client.GetAsync($"https://{ipAddress}/");
            
            // Tapo cameras typically return 404 or specific error patterns
            var isTapo = response.StatusCode == System.Net.HttpStatusCode.NotFound || 
                         response.Headers.Server?.ToString().Contains("TP-LINK", StringComparison.OrdinalIgnoreCase) == true;
                         
            if (isTapo)
            {
                System.Diagnostics.Debug.WriteLine($"HTTPS CHECK: {ipAddress} - TAPO signature detected");
            }
            
            return isTapo;
        }
        catch (HttpRequestException)
        {
            // This might actually indicate a Tapo camera (they often reject generic HTTP requests)
            System.Diagnostics.Debug.WriteLine($"HTTPS CHECK: {ipAddress} - HTTP exception (might be TAPO)");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HTTPS CHECK: {ipAddress} - {ex.Message}");
            return false;
        }
    }

    private async Task<bool> CheckTapoHTTP(string ipAddress)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(3);
            
            var response = await client.GetAsync($"http://{ipAddress}/");
            
            // Check for TP-Link/TAPO signatures in headers or response
            var responseText = await response.Content.ReadAsStringAsync();
            var isTapo = responseText.Contains("TP-LINK", StringComparison.OrdinalIgnoreCase) ||
                        responseText.Contains("TAPO", StringComparison.OrdinalIgnoreCase) ||
                        response.Headers.Server?.ToString().Contains("TP-LINK", StringComparison.OrdinalIgnoreCase) == true;
                        
            if (isTapo)
            {
                System.Diagnostics.Debug.WriteLine($"HTTP CHECK: {ipAddress} - TAPO signature detected");
            }
            
            return isTapo;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP CHECK: {ipAddress} - {ex.Message}");
            return false;
        }
    }

    private async Task<bool> CheckONVIFCapabilities(string ipAddress)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            
            // ONVIF GetCapabilities request
            var onvifRequest = @"<?xml version=""1.0"" encoding=""utf-8""?>
<soap-env:Envelope xmlns:soap-env=""http://www.w3.org/2003/05/soap-envelope"">
    <soap-env:Body>
        <GetCapabilities xmlns=""http://www.onvif.org/ver10/device/wsdl""/>
    </soap-env:Body>
</soap-env:Envelope>";

            var content = new StringContent(onvifRequest, Encoding.UTF8, "text/xml");
            var response = await client.PostAsync($"http://{ipAddress}:2020/onvif/service", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseText = await response.Content.ReadAsStringAsync();
                // Check if response contains ONVIF camera capabilities
                var isCamera = responseText.Contains("Media") || responseText.Contains("PTZ") || 
                              responseText.Contains("Analytics") || responseText.Contains("Imaging");
                              
                if (isCamera)
                {
                    System.Diagnostics.Debug.WriteLine($"ONVIF CHECK: {ipAddress} - Camera capabilities detected");
                }
                
                return isCamera;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ONVIF CHECK: {ipAddress} - {ex.Message}");
        }
        
        return false;
    }
    
    private async Task ScanDeviceAsync(string ipAddress, List<DiscoveredDevice> devices)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ipAddress, 1000);
            
            if (reply.Status == IPStatus.Success)
            {
                // Try to get device info to verify it's a Tapo device
                var deviceInfo = await GetDeviceInfoAsync(ipAddress);
                if (deviceInfo != null && !string.IsNullOrEmpty(deviceInfo.Model))
                {
                    var deviceType = DetermineDeviceType(deviceInfo.Model);
                    lock (devices)
                    {
                        devices.Add(new DiscoveredDevice
                        {
                            DeviceInfo = deviceInfo,
                            DeviceType = deviceType,
                            IpAddress = ipAddress
                        });
                    }
                }
            }
        }
        catch
        {
            // Device not reachable or not a Tapo device
        }
    }

    // Enhanced Discovery Methods from NetworkDiscovery Tool
    private async Task DiscoverTapoDevicesUDP(List<DiscoveredDevice> devices, int port, int timeoutSeconds)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"=== UDP DISCOVERY PORT {port} ===");
            
            using var client = new UdpClient(0); // Bind to any available port
            client.EnableBroadcast = true;
            
            var localEndpoint = (IPEndPoint)client.Client.LocalEndPoint!;
            System.Diagnostics.Debug.WriteLine($"UDP {port}: Listening on local port {localEndpoint.Port}");
            
            // Tapo discovery payloads based on known working formats from NetworkDiscovery tool
            var discoveryPayloads = new[]
            {
                // TP-Link/Tapo standard discovery format (most common)
                Encoding.UTF8.GetBytes("{\"system\":{\"get_sysinfo\":{}}}"),
                
                // Tapo camera discovery format
                Encoding.UTF8.GetBytes("{\"method\":\"get_device_info\",\"params\":{}}"),
                
                // Alternative Tapo format used by some apps
                Encoding.UTF8.GetBytes("{\"method\":\"handshake\",\"params\":{\"key\":\"\",\"requestTimeMils\":0}}"),
                
                // Simple discovery ping
                Encoding.UTF8.GetBytes("{\"method\":\"get_device_usage\"}"),
                
                // Basic TP-Link format
                Encoding.UTF8.GetBytes("{\"system\":{\"get_sysinfo\":null}}"),
                
                // Minimal discovery request
                Encoding.UTF8.GetBytes("{\"method\":\"login_device\"}"),
                
                // Empty JSON (some devices respond to any valid JSON)
                Encoding.UTF8.GetBytes("{}"),
                
                // Camera-specific discovery for newer models
                Encoding.UTF8.GetBytes("{\"method\":\"multipleRequest\",\"params\":{\"requests\":[{\"method\":\"get_device_info\"}]}}"),
                
                // ONVIF-style discovery for cameras
                Encoding.UTF8.GetBytes("{\"method\":\"get_capability\",\"params\":{}}")
            };

            var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, port);
            
            // Send discovery packets
            var sendTask = Task.Run(async () =>
            {
                for (int round = 0; round < 3; round++) // Send 3 rounds for better coverage
                {
                    System.Diagnostics.Debug.WriteLine($"UDP {port}: Sending round {round + 1} of discovery packets");
                    
                    foreach (var payload in discoveryPayloads)
                    {
                        try
                        {
                            await client.SendAsync(payload, broadcastEndpoint);
                            System.Diagnostics.Debug.WriteLine($"UDP {port}: Sent {payload.Length} bytes");
                            await Task.Delay(50); // Small delay between packets
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"UDP {port}: Send failed: {ex.Message}");
                        }
                    }
                    
                    await Task.Delay(500); // Delay between rounds
                }
            });

            // Listen for responses
            var listenTask = Task.Run(async () =>
            {
                var timeout = TimeSpan.FromSeconds(Math.Max(timeoutSeconds, 10)); // Minimum 10 seconds for cameras
                var startTime = DateTime.UtcNow;
                var responseCount = 0;
                
                System.Diagnostics.Debug.WriteLine($"UDP {port}: Listening for {timeout.TotalSeconds} seconds...");
                
                while (DateTime.UtcNow - startTime < timeout)
                {
                    try
                    {
                        var result = await client.ReceiveAsync().WaitAsync(TimeSpan.FromMilliseconds(1000));
                        responseCount++;
                        
                        System.Diagnostics.Debug.WriteLine($"UDP {port}: Response #{responseCount} from {result.RemoteEndPoint.Address}:{result.RemoteEndPoint.Port}");
                        await ProcessTapoUDPResponse(result.Buffer, result.RemoteEndPoint.Address.ToString(), port, devices);
                    }
                    catch (TimeoutException)
                    {
                        continue;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"UDP {port}: Error receiving response: {ex.Message}");
                        break;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"UDP {port}: Discovery completed. Received {responseCount} responses.");
            });

            await Task.WhenAll(sendTask, listenTask);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UDP discovery on port {port} failed: {ex.Message}");
        }
    }

    private async Task ProcessTapoUDPResponse(byte[] buffer, string ipAddress, int port, List<DiscoveredDevice> devices)
    {
        try
        {
            var response = Encoding.UTF8.GetString(buffer);
            System.Diagnostics.Debug.WriteLine($"UDP response from {ipAddress}:{port}");
            
            if (response.StartsWith("{") && response.EndsWith("}"))
            {
                try
                {
                    var jsonDoc = JsonDocument.Parse(response);
                    var root = jsonDoc.RootElement;
                    
                    var deviceInfo = new DeviceInfo { IpAddress = ipAddress };
                    bool foundDeviceInfo = false;
                    
                    // Try different JSON structure paths
                    if (root.TryGetProperty("system", out var system) && system.TryGetProperty("get_sysinfo", out var sysinfo))
                    {
                        ExtractTapoDeviceInfoFromJson(deviceInfo, sysinfo);
                        foundDeviceInfo = true;
                    }
                    else if (root.TryGetProperty("result", out var result))
                    {
                        ExtractTapoDeviceInfoFromJson(deviceInfo, result);
                        foundDeviceInfo = true;
                    }
                    else
                    {
                        ExtractTapoDeviceInfoFromJson(deviceInfo, root);
                        foundDeviceInfo = true;
                    }

                    if (foundDeviceInfo && !string.IsNullOrEmpty(deviceInfo.Model))
                    {
                        var deviceType = DetermineDeviceType(deviceInfo.Model);
                        
                        lock (devices)
                        {
                            if (!devices.Any(d => d.IpAddress == ipAddress))
                            {
                                devices.Add(new DiscoveredDevice
                                {
                                    DeviceInfo = deviceInfo,
                                    DeviceType = deviceType,
                                    IpAddress = ipAddress
                                });
                                System.Diagnostics.Debug.WriteLine($"Found Tapo device via UDP: {ipAddress} - {deviceInfo.Model}");
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid JSON from {ipAddress}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing UDP response from {ipAddress}: {ex.Message}");
        }
    }

    private void ExtractTapoDeviceInfoFromJson(DeviceInfo deviceInfo, JsonElement json)
    {
        try
        {
            if (json.TryGetProperty("mac", out var mac))
                deviceInfo.MacAddress = mac.GetString();
            else if (json.TryGetProperty("ethernet_mac", out var ethernetMac))
                deviceInfo.MacAddress = ethernetMac.GetString();
                
            if (json.TryGetProperty("device_id", out var deviceId))
                deviceInfo.DeviceId = deviceId.GetString() ?? string.Empty;
                
            if (json.TryGetProperty("model", out var model))
                deviceInfo.Model = model.GetString() ?? string.Empty;
                
            if (json.TryGetProperty("sw_ver", out var swVer))
                deviceInfo.SoftwareVersion = swVer.GetString();
            else if (json.TryGetProperty("fw_ver", out var fwVer))
                deviceInfo.SoftwareVersion = fwVer.GetString();
                
            if (json.TryGetProperty("alias", out var alias))
                deviceInfo.Alias = alias.GetString();
            else if (json.TryGetProperty("dev_name", out var devName))
                deviceInfo.Alias = devName.GetString();
                
            if (json.TryGetProperty("hw_ver", out var hwVersion))
                deviceInfo.HardwareVersion = hwVersion.GetString();

            // Handle nested device info if present
            if (json.TryGetProperty("device_info", out var nestedDeviceInfo))
            {
                ExtractTapoDeviceInfoFromJson(deviceInfo, nestedDeviceInfo);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error extracting device info: {ex.Message}");
        }
    }

    private async Task DiscoverTapoDevicesMDNS(List<DiscoveredDevice> devices, int timeoutSeconds)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("mDNS discovery...");
            
            using var client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            
            var multicastAddress = IPAddress.Parse("224.0.0.251");
            var multicastEndpoint = new IPEndPoint(multicastAddress, 5353);
            
            client.JoinMulticastGroup(multicastAddress);
            
            // mDNS queries for Tapo devices
            var queries = new[]
            {
                "_hap._tcp.local",      // HomeKit devices
                "_http._tcp.local",     // HTTP services
                "_tplink._tcp.local"    // TP-Link services
            };

            foreach (var query in queries)
            {
                var packet = CreateSimpleMDNSQuery(query);
                await client.SendAsync(packet, multicastEndpoint);
                await Task.Delay(200);
            }

            // Listen for mDNS responses
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);
            var startTime = DateTime.UtcNow;
            
            while (DateTime.UtcNow - startTime < timeout)
            {
                try
                {
                    var result = await client.ReceiveAsync().WaitAsync(TimeSpan.FromMilliseconds(500));
                    await ProcessMDNSResponse(result.Buffer, result.RemoteEndPoint.Address.ToString(), devices);
                }
                catch (TimeoutException)
                {
                    continue;
                }
                catch (Exception)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"mDNS discovery failed: {ex.Message}");
        }
    }

    private byte[] CreateSimpleMDNSQuery(string service)
    {
        var query = new List<byte>();
        
        // Simple mDNS query structure
        query.AddRange(BitConverter.GetBytes((ushort)0x0000)); // Transaction ID
        query.AddRange(BitConverter.GetBytes((ushort)0x0000)); // Flags
        query.AddRange(BitConverter.GetBytes((ushort)0x0001)); // Questions
        query.AddRange(BitConverter.GetBytes((ushort)0x0000)); // Answer RRs
        query.AddRange(BitConverter.GetBytes((ushort)0x0000)); // Authority RRs  
        query.AddRange(BitConverter.GetBytes((ushort)0x0000)); // Additional RRs
        
        // Service name
        var parts = service.Split('.');
        foreach (var part in parts)
        {
            if (!string.IsNullOrEmpty(part))
            {
                query.Add((byte)part.Length);
                query.AddRange(Encoding.UTF8.GetBytes(part));
            }
        }
        query.Add(0); // End of name
        
        query.AddRange(BitConverter.GetBytes((ushort)0x000C)); // Type PTR
        query.AddRange(BitConverter.GetBytes((ushort)0x0001)); // Class IN
        
        return query.ToArray();
    }

    private async Task ProcessMDNSResponse(byte[] buffer, string ipAddress, List<DiscoveredDevice> devices)
    {
        try
        {
            var response = Encoding.UTF8.GetString(buffer);
            
            var indicators = new[] { "tapo", "tplink", "tp-link", "Tapo_", "TP-Link" };
            var foundIndicator = indicators.FirstOrDefault(indicator => 
                response.Contains(indicator, StringComparison.OrdinalIgnoreCase));
            
            if (foundIndicator != null)
            {
                var deviceInfo = await GetDeviceInfoAsync(ipAddress);
                if (deviceInfo != null && !string.IsNullOrEmpty(deviceInfo.Model))
                {
                    var deviceType = DetermineDeviceType(deviceInfo.Model);
                    
                    lock (devices)
                    {
                        if (!devices.Any(d => d.IpAddress == ipAddress))
                        {
                            devices.Add(new DiscoveredDevice
                            {
                                DeviceInfo = deviceInfo,
                                DeviceType = deviceType,
                                IpAddress = ipAddress
                            });
                            System.Diagnostics.Debug.WriteLine($"Found Tapo device via mDNS: {ipAddress} - {deviceInfo.Model}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing mDNS response from {ipAddress}: {ex.Message}");
        }
    }

    private async Task ScanNetworkForTapoDevices(List<DiscoveredDevice> devices, string targetNetwork, int timeoutSeconds)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"=== NETWORK SCAN: {targetNetwork} ===");
            
            var baseIp = targetNetwork.Replace(".255", "");
            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(30); // Increase concurrent connections
            
            System.Diagnostics.Debug.WriteLine($"Scanning {baseIp}.1 to {baseIp}.254...");
            
            for (int i = 1; i <= 254; i++)
            {
                var ip = $"{baseIp}.{i}";
                tasks.Add(ScanForTapoDevice(ip, devices, semaphore, timeoutSeconds));
            }
            
            await Task.WhenAll(tasks);
            System.Diagnostics.Debug.WriteLine($"Network scan completed for {targetNetwork}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Network scan error for {targetNetwork}: {ex.Message}");
        }
    }

    private async Task ScanForTapoDevice(string ipAddress, List<DiscoveredDevice> devices, SemaphoreSlim semaphore, int timeoutSeconds)
    {
        await semaphore.WaitAsync();
        
        try
        {
            // First do a ping to see if device is reachable
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ipAddress, 1000);
            
            if (reply.Status == IPStatus.Success)
            {
                System.Diagnostics.Debug.WriteLine($"PING SUCCESS: {ipAddress} ({reply.RoundtripTime}ms)");
                
                // Check for Tapo camera signatures first (most likely to be missed)
                if (await IsTapoCameraDevice(ipAddress))
                {
                    System.Diagnostics.Debug.WriteLine($"CAMERA DETECTED: {ipAddress}");
                    
                    var deviceInfo = new DeviceInfo 
                    { 
                        IpAddress = ipAddress,
                        Model = "Unknown Tapo Camera",
                        DeviceOn = false
                    };
                    
                    var deviceType = DeviceType.CameraPtz;
                    
                    lock (devices)
                    {
                        if (!devices.Any(d => d.IpAddress == ipAddress))
                        {
                            devices.Add(new DiscoveredDevice
                            {
                                DeviceInfo = deviceInfo,
                                DeviceType = deviceType,
                                IpAddress = ipAddress
                            });
                            System.Diagnostics.Debug.WriteLine($"ADDED CAMERA: {ipAddress}");
                        }
                    }
                }
                else
                {
                    // Try to get device info for other Tapo devices
                    try 
                    {
                        var deviceInfo = await GetDeviceInfoAsync(ipAddress);
                        if (deviceInfo != null && !string.IsNullOrEmpty(deviceInfo.Model))
                        {
                            System.Diagnostics.Debug.WriteLine($"DEVICE INFO SUCCESS: {ipAddress} - {deviceInfo.Model}");
                            
                            var deviceType = DetermineDeviceType(deviceInfo.Model);
                            lock (devices)
                            {
                                if (!devices.Any(d => d.IpAddress == ipAddress))
                                {
                                    devices.Add(new DiscoveredDevice
                                    {
                                        DeviceInfo = deviceInfo,
                                        DeviceType = deviceType,
                                        IpAddress = ipAddress
                                    });
                                    System.Diagnostics.Debug.WriteLine($"ADDED DEVICE: {ipAddress} - {deviceInfo.Model}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"GetDeviceInfo failed for {ipAddress}: {ex.Message}");
                    }
                }
            }
        }
        catch
        {
            // Ignore ping failures - device not reachable
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<bool> IsTapoCameraDevice(string ipAddress)
    {
        System.Diagnostics.Debug.WriteLine($"Checking camera signatures for {ipAddress}...");
        
        // Check for Tapo camera indicators
        var tapoIndicators = new[]
        {
            (443, "HTTPS"),    // Modern Tapo cameras use HTTPS
            (2020, "ONVIF"),   // ONVIF protocol for cameras
            (8800, "TP-Link"), // TP-Link proprietary streaming
            (554, "RTSP")      // RTSP streaming
        };

        int tapoPortCount = 0;
        var openPorts = new List<int>();
        
        foreach (var (port, protocol) in tapoIndicators)
        {
            if (await IsPortOpen(ipAddress, port, 200))
            {
                tapoPortCount++;
                openPorts.Add(port);
                System.Diagnostics.Debug.WriteLine($"CAMERA CHECK: {ipAddress}:{port} ({protocol}) - OPEN");
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"CAMERA CHECK: {ipAddress} has {tapoPortCount}/4 camera ports: [{string.Join(",", openPorts)}]");

        // If device has 2+ Tapo-specific ports, it's likely a Tapo camera
        if (tapoPortCount >= 2)
        {
            System.Diagnostics.Debug.WriteLine($"CAMERA CONFIRMED: {ipAddress} (has {tapoPortCount} camera ports)");
            return true;
        }
        
        // Also check for just HTTPS + one other (common for newer cameras)
        if (openPorts.Contains(443) && tapoPortCount >= 1)
        {
            System.Diagnostics.Debug.WriteLine($"CAMERA POSSIBLE: {ipAddress} (has HTTPS + {tapoPortCount-1} other camera ports)");
            return true;
        }

        return false;
    }

    private async Task<bool> IsPortOpen(string host, int port, int timeout)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(timeout);
            
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            
            return completedTask == connectTask && client.Connected;
        }
        catch
        {
            return false;
        }
    }
    
    private static DeviceType DetermineDeviceType(string model)
    {
        return model.ToUpper() switch
        {
            var m when m.StartsWith("L5") && (m.Contains("30") || m.Contains("35")) => DeviceType.ColorLight,
            var m when m.StartsWith("L5") => DeviceType.Light,
            var m when m.StartsWith("L6") && (m.Contains("30") || m.Contains("35")) => DeviceType.ColorLight,
            var m when m.StartsWith("L6") => DeviceType.Light,
            var m when m.StartsWith("L9") => DeviceType.RgbLightStrip,
            var m when m.StartsWith("P1") && (m.Contains("110") || m.Contains("115")) => DeviceType.PlugEnergyMonitoring,
            var m when m.StartsWith("P1") => DeviceType.Plug,
            var m when m.StartsWith("P3") => DeviceType.PowerStrip,
            var m when m.StartsWith("H1") || m.StartsWith("H2") => DeviceType.Hub,
            var m when m.StartsWith("C2") || m.StartsWith("C3") => DeviceType.CameraPtz,
            var m when m.StartsWith("C4") || m.StartsWith("C5") => DeviceType.CameraPtz,
            var m when m.StartsWith("TC") => DeviceType.CameraPtz, // TC40, TC70 etc.
            var m when m.Contains("CAMERA") => DeviceType.CameraPtz,
            var m when m.StartsWith("D2") || m.StartsWith("D1") => DeviceType.CameraPtz, // Video Doorbells
            var m when m.StartsWith("S2") => DeviceType.Switch, // Smart switches
            var m when m.StartsWith("T1") || m.StartsWith("T3") => DeviceType.Sensor, // Smart sensors
            _ => DeviceType.Unknown
        };
    }

    public async Task<DeviceInfo> GetDeviceInfoAsync(string ipAddress)
    {
        try
        {
            var request = new TapoRequest
            {
                Method = "get_device_info"
            };
            
            var response = await SendRequestAsync<DeviceInfo>(ipAddress, request);
            if (response?.Result != null)
            {
                response.Result.IpAddress = ipAddress;
                return response.Result;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get device info error: {ex.Message}");
        }
        
        return new DeviceInfo { IpAddress = ipAddress };
    }

    public async Task<bool> TurnOnAsync(string ipAddress)
    {
        var request = new TapoRequest
        {
            Method = "set_device_info",
            Params = new SetDeviceInfoParams { DeviceOn = true }
        };
        
        var response = await SendRequestAsync<object>(ipAddress, request);
        return response?.ErrorCode == 0;
    }

    public async Task<bool> TurnOffAsync(string ipAddress)
    {
        var request = new TapoRequest
        {
            Method = "set_device_info",
            Params = new SetDeviceInfoParams { DeviceOn = false }
        };
        
        var response = await SendRequestAsync<object>(ipAddress, request);
        return response?.ErrorCode == 0;
    }

    public async Task<bool> SetBrightnessAsync(string ipAddress, int brightness)
    {
        var request = new TapoRequest
        {
            Method = "set_device_info",
            Params = new SetDeviceInfoParams { Brightness = Math.Clamp(brightness, 1, 100) }
        };
        
        var response = await SendRequestAsync<object>(ipAddress, request);
        return response?.ErrorCode == 0;
    }

    public async Task<bool> SetColorAsync(string ipAddress, Color color)
    {
        return await SetHueSaturationAsync(ipAddress, color.Hue, color.Saturation);
    }

    public async Task<bool> SetHueSaturationAsync(string ipAddress, int hue, int saturation)
    {
        var request = new TapoRequest
        {
            Method = "set_device_info",
            Params = new SetDeviceInfoParams 
            { 
                Hue = Math.Clamp(hue, 0, 360),
                Saturation = Math.Clamp(saturation, 0, 100)
            }
        };
        
        var response = await SendRequestAsync<object>(ipAddress, request);
        return response?.ErrorCode == 0;
    }

    public async Task<bool> SetColorTemperatureAsync(string ipAddress, int colorTemp)
    {
        var request = new TapoRequest
        {
            Method = "set_device_info",
            Params = new SetDeviceInfoParams { ColorTemp = Math.Clamp(colorTemp, 2500, 6500) }
        };
        
        var response = await SendRequestAsync<object>(ipAddress, request);
        return response?.ErrorCode == 0;
    }

    public async Task<EnergyUsage?> GetEnergyUsageAsync(string ipAddress)
    {
        try
        {
            var request = new TapoRequest
            {
                Method = "get_energy_usage"
            };
            
            var response = await SendRequestAsync<EnergyUsage>(ipAddress, request);
            return response?.Result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get energy usage error: {ex.Message}");
            return null;
        }
    }

    public async Task<CurrentPower?> GetCurrentPowerAsync(string ipAddress)
    {
        try
        {
            var request = new TapoRequest
            {
                Method = "get_current_power"
            };
            
            var response = await SendRequestAsync<CurrentPower>(ipAddress, request);
            return response?.Result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Get current power error: {ex.Message}");
            return null;
        }
    }

    private async Task<TapoResponse<T>?> SendRequestAsync<T>(string ipAddress, TapoRequest request)
    {
        try
        {
            // This is a simplified implementation. In a real scenario, you'd need:
            // 1. Authentication handshake
            // 2. Encryption/decryption of requests
            // 3. Session management
            
            var url = $"http://{ipAddress}/app";
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var tapoResponse = JsonSerializer.Deserialize<TapoResponse<T>>(responseJson);
                return tapoResponse;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Request error: {ex.Message}");
        }
        
        return null;
    }
}