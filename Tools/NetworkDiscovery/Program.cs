using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace NetworkDiscovery;

class Program
{
    private static readonly object lockObj = new object();
    private static readonly List<NetworkDevice> discoveredDevices = new List<NetworkDevice>();
    private static readonly List<TapoDevice> discoveredTapoDevices = new List<TapoDevice>();

    static async Task Main(string[] args)
    {
        Console.WriteLine("Enhanced Network Discovery Tool with Tapo Detection");
        Console.WriteLine("==================================================");
        Console.WriteLine();

        try
        {
            // Get local network interfaces
            var localIPs = GetLocalNetworkInterfaces();
            
            if (!localIPs.Any())
            {
                Console.WriteLine("No active network interfaces found.");
                return;
            }

            Console.WriteLine("Active Network Interfaces:");
            foreach (var ip in localIPs)
            {
                Console.WriteLine($"  {ip}");
            }
            Console.WriteLine();

            // Start Tapo-specific discovery methods
            Console.WriteLine("Starting Tapo device discovery...");
            var tapoTasks = new List<Task>
            {
                DiscoverTapoDevicesUDP(9999),    // Smart plugs/bulbs (legacy)
                DiscoverTapoDevicesUDP(20002),   // Cameras (legacy)
                DiscoverTapoDevicesMDNS(),       // mDNS discovery
                SendTapoDiscoveryProbes()        // New: Send discovery probes to activate local APIs
            };

            // Start general network scanning
            Console.WriteLine("Starting general network scan...");
            var scanTasks = new List<Task>();
            foreach (var localIP in localIPs)
            {
                var networkBase = GetNetworkBase(localIP);
                if (networkBase != null)
                {
                    Console.WriteLine($"Scanning network: {networkBase}/24");
                    scanTasks.Add(ScanNetwork(networkBase));
                }
            }

            // Run both Tapo discovery and general scanning concurrently
            await Task.WhenAll(tapoTasks.Concat(scanTasks));

            // Display results
            DisplayResults();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static async Task DiscoverTapoDevicesUDP(int port)
    {
        try
        {
            Console.WriteLine($"Scanning for Tapo devices on UDP port {port}...");
            
            // Create UDP client bound to a specific local port for receiving responses
            using var client = new UdpClient(0); // Bind to any available port
            client.EnableBroadcast = true;
            
            var localEndpoint = (IPEndPoint)client.Client.LocalEndPoint!;
            Console.WriteLine($"Listening for responses on local port {localEndpoint.Port}");
            
            // Tapo discovery payloads based on known working formats
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
                
                // Plain text discovery (fallback)
                Encoding.UTF8.GetBytes("M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1900\r\nMAN: \"ssdp:discover\"\r\nST: upnp:rootdevice\r\nMX: 3\r\n\r\n")
            };

            var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, port);
            
            // Send discovery packets with delays
            var sendTask = Task.Run(async () =>
            {
                foreach (var payload in discoveryPayloads)
                {
                    try
                    {
                        await client.SendAsync(payload, broadcastEndpoint);
                        Console.WriteLine($"Sent discovery packet to broadcast:{port} ({payload.Length} bytes)");
                        await Task.Delay(150); // Delay between different payloads
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send discovery packet: {ex.Message}");
                    }
                }
                
                // Send a second round after a brief pause
                await Task.Delay(500);
                foreach (var payload in discoveryPayloads.Take(2)) // Just the first two payloads
                {
                    try
                    {
                        await client.SendAsync(payload, broadcastEndpoint);
                        await Task.Delay(100);
                    }
                    catch { }
                }
            });

            // Listen for unicast responses from Tapo devices
            var listenTask = Task.Run(async () =>
            {
                var timeout = TimeSpan.FromSeconds(5); // Give more time for responses
                var startTime = DateTime.UtcNow;
                var responseCount = 0;
                
                while (DateTime.UtcNow - startTime < timeout)
                {
                    try
                    {
                        var result = await client.ReceiveAsync().WaitAsync(TimeSpan.FromMilliseconds(800));
                        responseCount++;
                        
                        Console.WriteLine($"Received response #{responseCount} from {result.RemoteEndPoint.Address}:{result.RemoteEndPoint.Port}");
                        ProcessTapoUDPResponse(result.Buffer, result.RemoteEndPoint.Address.ToString(), port, result.RemoteEndPoint.Port);
                    }
                    catch (TimeoutException)
                    {
                        // Continue listening - this is normal
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error receiving UDP response: {ex.Message}");
                        break;
                    }
                }
                
                Console.WriteLine($"UDP discovery on port {port} completed. Received {responseCount} responses.");
            });

            // Wait for both sending and listening to complete
            await Task.WhenAll(sendTask, listenTask);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UDP discovery on port {port} failed: {ex.Message}");
        }
    }

    static void ProcessTapoUDPResponse(byte[] buffer, string ipAddress, int discoveryPort, int responsePort)
    {
        try
        {
            var response = Encoding.UTF8.GetString(buffer);
            Console.WriteLine($"Processing response from {ipAddress}:{responsePort} (discovery port {discoveryPort})");
            
            // Log first part of response for debugging
            var preview = response.Length > 100 ? response[..100] + "..." : response;
            Console.WriteLine($"Response preview: {preview}");
            
            // Try to parse as JSON
            if (response.StartsWith("{") && response.EndsWith("}"))
            {
                try
                {
                    var jsonDoc = JsonDocument.Parse(response);
                    var root = jsonDoc.RootElement;
                    
                    var device = new TapoDevice
                    {
                        IPAddress = ipAddress,
                        Port = discoveryPort,
                        ResponsePort = responsePort,
                        DiscoveryMethod = $"UDP Broadcast (Port {discoveryPort})",
                        RawResponse = response.Length > 1000 ? response[..1000] + "..." : response
                    };

                    // Extract device information from JSON
                    bool foundDeviceInfo = false;
                    
                    // Try different JSON structure paths
                    if (root.TryGetProperty("system", out var system))
                    {
                        if (system.TryGetProperty("get_sysinfo", out var sysinfo))
                        {
                            ExtractTapoDeviceInfo(device, sysinfo);
                            foundDeviceInfo = true;
                        }
                    }
                    
                    if (!foundDeviceInfo && root.TryGetProperty("result", out var result))
                    {
                        ExtractTapoDeviceInfo(device, result);
                        foundDeviceInfo = true;
                    }
                    
                    if (!foundDeviceInfo)
                    {
                        // Try to extract any available info from root
                        ExtractTapoDeviceInfo(device, root);
                        foundDeviceInfo = true;
                    }

                    // If we got a structured JSON response, it's likely a Tapo device
                    if (foundDeviceInfo)
                    {
                        lock (lockObj)
                        {
                            // Avoid duplicates based on IP address
                            if (!discoveredTapoDevices.Any(d => d.IPAddress == ipAddress))
                            {
                                discoveredTapoDevices.Add(device);
                                Console.WriteLine($"✅ Found Tapo device: {ipAddress} - {device.Model ?? "Unknown Model"} via UDP:{discoveryPort}");
                            }
                            else
                            {
                                Console.WriteLine($"📝 Updated existing Tapo device info for {ipAddress}");
                                // Update existing device with additional info
                                var existing = discoveredTapoDevices.First(d => d.IPAddress == ipAddress);
                                if (string.IsNullOrEmpty(existing.Model) && !string.IsNullOrEmpty(device.Model))
                                    existing.Model = device.Model;
                                if (string.IsNullOrEmpty(existing.DeviceType) && !string.IsNullOrEmpty(device.DeviceType))
                                    existing.DeviceType = device.DeviceType;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Got JSON response but couldn't extract device info from {ipAddress}");
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"❌ Invalid JSON from {ipAddress}: {ex.Message}");
                }
            }
            else
            {
                // Non-JSON response, still might be a Tapo device with a different protocol
                Console.WriteLine($"📡 Non-JSON response from {ipAddress} - might be encrypted or different format");
                
                var device = new TapoDevice
                {
                    IPAddress = ipAddress,
                    Port = discoveryPort,
                    ResponsePort = responsePort,
                    DiscoveryMethod = $"UDP Broadcast (Port {discoveryPort}) - Non-JSON",
                    RawResponse = response.Length > 200 ? response[..200] + "..." : response,
                    DeviceType = "Potential Tapo Device (Non-JSON Response)"
                };

                lock (lockObj)
                {
                    if (!discoveredTapoDevices.Any(d => d.IPAddress == ipAddress))
                    {
                        discoveredTapoDevices.Add(device);
                        Console.WriteLine($"❓ Found potential Tapo device: {ipAddress} via UDP:{discoveryPort} (non-JSON response)");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error processing UDP response from {ipAddress}: {ex.Message}");
        }
    }

    static void ExtractTapoDeviceInfo(TapoDevice device, JsonElement json)
    {
        try
        {
            // Primary Tapo device fields
            if (json.TryGetProperty("mac", out var mac))
                device.MacAddress = mac.GetString();
            else if (json.TryGetProperty("ethernet_mac", out var ethernetMac))
                device.MacAddress = ethernetMac.GetString();
            else if (json.TryGetProperty("hw_id", out var hwId))
                device.MacAddress = hwId.GetString();
                
            if (json.TryGetProperty("device_id", out var deviceId))
                device.DeviceId = deviceId.GetString();
            else if (json.TryGetProperty("deviceId", out var deviceId2))
                device.DeviceId = deviceId2.GetString();
                
            if (json.TryGetProperty("model", out var model))
                device.Model = model.GetString();
            else if (json.TryGetProperty("hw_ver", out var hwVer))
                device.HardwareVersion = hwVer.GetString();
                
            if (json.TryGetProperty("device_type", out var deviceType))
                device.DeviceType = deviceType.GetString();
            else if (json.TryGetProperty("type", out var type))
                device.DeviceType = type.GetString();
                
            if (json.TryGetProperty("sw_ver", out var swVer))
                device.SoftwareVersion = swVer.GetString();
            else if (json.TryGetProperty("fw_ver", out var fwVer))
                device.SoftwareVersion = fwVer.GetString();
                
            if (json.TryGetProperty("alias", out var alias))
                device.Alias = alias.GetString();
            else if (json.TryGetProperty("dev_name", out var devName))
                device.Alias = devName.GetString();
                
            if (json.TryGetProperty("nickname", out var nickname))
                device.Nickname = nickname.GetString();

            // Alternative property names and nested structures
            if (json.TryGetProperty("hw_ver", out var hwVersion) && string.IsNullOrEmpty(device.HardwareVersion))
                device.HardwareVersion = hwVersion.GetString();
                
            // Handle TP-Link specific response format
            if (json.TryGetProperty("mic_type", out var micType))
                device.DeviceType ??= $"Tapo Device ({micType.GetString()})";
                
            // Extract from nested device info if present
            if (json.TryGetProperty("device_info", out var deviceInfo))
            {
                ExtractTapoDeviceInfo(device, deviceInfo); // Recursive call for nested info
            }
            
            // Set default device type if we found MAC or device_id but no explicit type
            if (string.IsNullOrEmpty(device.DeviceType) && 
                (!string.IsNullOrEmpty(device.MacAddress) || !string.IsNullOrEmpty(device.DeviceId)))
            {
                device.DeviceType = "Tapo Device";
            }
            
            // Enhance device type based on model if we have it
            if (!string.IsNullOrEmpty(device.Model))
            {
                device.DeviceType = device.Model.ToUpper() switch
                {
                    var m when m.StartsWith("P1") => "Tapo Smart Plug",
                    var m when m.StartsWith("L5") => "Tapo Smart Bulb",
                    var m when m.StartsWith("L9") => "Tapo Smart Light Strip",
                    var m when m.StartsWith("C2") || m.StartsWith("C3") => "Tapo Security Camera",
                    var m when m.StartsWith("D2") || m.StartsWith("D1") => "Tapo Video Doorbell",
                    var m when m.StartsWith("S2") => "Tapo Smart Switch",
                    var m when m.StartsWith("H1") || m.StartsWith("H2") => "Tapo Smart Hub",
                    var m when m.StartsWith("T1") || m.StartsWith("T3") => "Tapo Smart Sensor",
                    _ => device.DeviceType ?? $"Tapo {device.Model}"
                };
            }

            Console.WriteLine($"🔍 Extracted info: Model={device.Model}, Type={device.DeviceType}, MAC={device.MacAddress?.Substring(0, Math.Min(8, device.MacAddress?.Length ?? 0))}...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error extracting device info: {ex.Message}");
        }
    }

    static async Task DiscoverTapoDevicesMDNS()
    {
        try
        {
            Console.WriteLine("Scanning for Tapo devices via mDNS...");
            
            using var client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            
            var multicastAddress = IPAddress.Parse("224.0.0.251");
            var multicastEndpoint = new IPEndPoint(multicastAddress, 5353);
            
            // Join multicast group
            client.JoinMulticastGroup(multicastAddress);
            
            // mDNS query for Tapo devices
            var queries = new[]
            {
                "_hap._tcp.local",      // HomeKit devices
                "_matterc._udp.local",  // Matter devices
                "_http._tcp.local",     // HTTP services
                "_tapo._tcp.local",     // Tapo-specific (if exists)
                "_tplink._tcp.local"    // TP-Link services
            };

            foreach (var query in queries)
            {
                var packet = CreateMDNSQuery(query);
                await client.SendAsync(packet, multicastEndpoint);
                await Task.Delay(200);
            }

            // Listen for mDNS responses
            var timeout = TimeSpan.FromSeconds(5);
            var startTime = DateTime.UtcNow;
            
            while (DateTime.UtcNow - startTime < timeout)
            {
                try
                {
                    var result = await client.ReceiveAsync().WaitAsync(TimeSpan.FromMilliseconds(500));
                    ProcessMDNSResponse(result.Buffer, result.RemoteEndPoint.Address.ToString());
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
            Console.WriteLine($"mDNS discovery failed: {ex.Message}");
        }
    }

    static byte[] CreateMDNSQuery(string service)
    {
        // This is a simplified mDNS query packet
        // In a production implementation, you'd want a proper mDNS library
        var query = new List<byte>();
        
        // Transaction ID (2 bytes)
        query.AddRange(BitConverter.GetBytes((ushort)0x0000));
        
        // Flags (2 bytes) - Standard query
        query.AddRange(BitConverter.GetBytes((ushort)0x0000));
        
        // Questions (2 bytes)
        query.AddRange(BitConverter.GetBytes((ushort)0x0001));
        
        // Answer RRs (2 bytes)
        query.AddRange(BitConverter.GetBytes((ushort)0x0000));
        
        // Authority RRs (2 bytes)
        query.AddRange(BitConverter.GetBytes((ushort)0x0000));
        
        // Additional RRs (2 bytes)
        query.AddRange(BitConverter.GetBytes((ushort)0x0000));
        
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
        
        // Type PTR (2 bytes)
        query.AddRange(BitConverter.GetBytes((ushort)0x000C));
        
        // Class IN (2 bytes)
        query.AddRange(BitConverter.GetBytes((ushort)0x0001));
        
        return query.ToArray();
    }

    static void ProcessMDNSResponse(byte[] buffer, string ipAddress)
    {
        try
        {
            var response = Encoding.UTF8.GetString(buffer);
            
            // Look for Tapo/TP-Link indicators in the response
            var indicators = new[] { "tapo", "tplink", "tp-link", "Tapo_", "TP-Link" };
            var foundIndicator = indicators.FirstOrDefault(indicator => 
                response.Contains(indicator, StringComparison.OrdinalIgnoreCase));
            
            if (foundIndicator != null)
            {
                var device = new TapoDevice
                {
                    IPAddress = ipAddress,
                    DiscoveryMethod = "mDNS Discovery",
                    DeviceType = "Tapo Device (mDNS)",
                    RawResponse = response.Length > 500 ? response[..500] + "..." : response
                };

                // Try to extract device info from mDNS response
                if (response.Contains("Tapo_Camera", StringComparison.OrdinalIgnoreCase))
                {
                    device.DeviceType = "Tapo Camera";
                    device.Model = ExtractModelFromMDNS(response, "Tapo_Camera");
                }
                else if (response.Contains("Tapo_Plug", StringComparison.OrdinalIgnoreCase))
                {
                    device.DeviceType = "Tapo Smart Plug";
                    device.Model = ExtractModelFromMDNS(response, "Tapo_Plug");
                }

                lock (lockObj)
                {
                    if (!discoveredTapoDevices.Any(d => d.IPAddress == ipAddress))
                    {
                        discoveredTapoDevices.Add(device);
                        Console.WriteLine($"Found Tapo device via mDNS: {ipAddress} - {device.DeviceType}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing mDNS response from {ipAddress}: {ex.Message}");
        }
    }

    static string ExtractModelFromMDNS(string response, string prefix)
    {
        try
        {
            var index = response.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var start = index + prefix.Length;
                var end = response.IndexOfAny(new[] { ' ', '\0', '\n', '\r', '.' }, start);
                if (end > start)
                {
                    return response[start..end];
                }
            }
        }
        catch { }
        
        return "Unknown";
    }

    static async Task SendTapoDiscoveryProbes()
    {
        try
        {
            Console.WriteLine("Sending Tapo discovery probes to activate local APIs...");
            
            // Get local network interfaces to determine which networks to probe
            var localIPs = GetLocalNetworkInterfaces();
            
            using var client = new UdpClient();
            client.EnableBroadcast = true;
            
            // Discovery probe packet as mentioned in the GitHub issue
            // This activates the local API on devices that need it
            var discoveryProbe = Encoding.UTF8.GetBytes("{\"system\":{\"get_sysinfo\":{}}}");
            
            foreach (var localIP in localIPs)
            {
                var networkBase = GetNetworkBase(localIP);
                if (networkBase != null)
                {
                    // Send probes to common Tapo device IP addresses in the subnet
                    var commonLastOctets = new[] { 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110 };
                    
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
            }
            
            // Also send broadcast probes
            await client.SendAsync(discoveryProbe, new IPEndPoint(IPAddress.Broadcast, 9999));
            await client.SendAsync(discoveryProbe, new IPEndPoint(IPAddress.Broadcast, 20002));
            
            Console.WriteLine("Discovery probes sent - this may activate dormant Tapo device APIs");
            
            // Wait a moment for devices to activate their APIs
            await Task.Delay(2000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Discovery probe sending failed: {ex.Message}");
        }
    }

    static List<IPAddress> GetLocalNetworkInterfaces()
    {
        var localIPs = new List<IPAddress>();

        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
            {
                if (ni.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(ip.Address))
                        {
                            localIPs.Add(ip.Address);
                        }
                    }
                }
            }
        }

        return localIPs;
    }

    static string? GetNetworkBase(IPAddress ip)
    {
        var octets = ip.ToString().Split('.');
        if (octets.Length == 4)
        {
            return $"{octets[0]}.{octets[1]}.{octets[2]}";
        }
        return null;
    }

    static async Task ScanNetwork(string networkBase)
    {
        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(50); // Limit concurrent connections

        for (int i = 1; i <= 254; i++)
        {
            var ip = $"{networkBase}.{i}";
            tasks.Add(ScanHost(ip, semaphore));
        }

        await Task.WhenAll(tasks);
    }

    static async Task ScanHost(string ipAddress, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        
        try
        {
            var ping = new Ping();
            var reply = await ping.SendPingAsync(ipAddress, 1000);

            if (reply.Status == IPStatus.Success)
            {
                var device = new NetworkDevice
                {
                    IPAddress = ipAddress,
                    ResponseTime = reply.RoundtripTime,
                    IsReachable = true
                };

                // Try to get hostname
                try
                {
                    var hostEntry = await Dns.GetHostEntryAsync(ipAddress);
                    device.HostName = hostEntry.HostName;
                }
                catch
                {
                    device.HostName = "Unknown";
                }

                // Try to detect device type based on common ports
                device.DeviceType = await DetectDeviceType(ipAddress);
                device.OpenPorts = await ScanCommonPorts(ipAddress);

                lock (lockObj)
                {
                    discoveredDevices.Add(device);
                }

                Console.WriteLine($"Found: {ipAddress} ({device.HostName}) - {reply.RoundtripTime}ms");
            }
        }
        catch (Exception)
        {
            // Ignore ping failures
        }
        finally
        {
            semaphore.Release();
        }
    }

    static async Task<string> DetectDeviceType(string ipAddress)
    {
        var commonPorts = new Dictionary<int, string>
        {
            { 22, "SSH Server" },
            { 23, "Telnet" },
            { 53, "DNS Server" },
            { 80, "Web Server" },
            { 135, "Windows RPC" },
            { 139, "NetBIOS" },
            { 443, "HTTPS Server" },
            { 445, "SMB/Windows File Sharing" },
            { 548, "Apple File Protocol" },
            { 631, "Printer" },
            { 993, "IMAPS" },
            { 995, "POP3S" },
            { 1900, "UPnP" },
            { 2020, "ONVIF Camera" },
            { 3389, "Remote Desktop" },
            { 5353, "Bonjour/mDNS" },
            { 8080, "HTTP Proxy/Web Server" },
            { 8800, "TP-Link Streaming" },
            { 9100, "Printer" }
        };

        // Check for Tapo camera signature first
        if (await IsTapoCamera(ipAddress))
        {
            return "Tapo Camera";
        }

        foreach (var port in commonPorts.Keys.Take(7)) // Check first 7 ports for speed
        {
            if (await IsPortOpen(ipAddress, port, 500))
            {
                return commonPorts[port];
            }
        }

        return "Unknown Device";
    }

    static async Task<bool> IsTapoCamera(string ipAddress)
    {
        // Check for Tapo camera indicators
        var tapoIndicators = new[]
        {
            (443, "HTTPS"),    // Modern Tapo cameras use HTTPS
            (2020, "ONVIF"),   // ONVIF protocol for cameras
            (8800, "TP-Link"), // TP-Link proprietary streaming
            (554, "RTSP")      // RTSP streaming
        };

        int tapoPortCount = 0;
        foreach (var (port, protocol) in tapoIndicators)
        {
            if (await IsPortOpen(ipAddress, port, 300))
            {
                tapoPortCount++;
                Console.WriteLine($"🔍 Found {protocol} port {port} on {ipAddress}");
            }
        }

        // If device has 2+ Tapo-specific ports, it's likely a Tapo camera
        if (tapoPortCount >= 2)
        {
            // Try ONVIF discovery if port 2020 is open
            if (await IsPortOpen(ipAddress, 2020, 300))
            {
                var isOnvifCamera = await CheckONVIFCapabilities(ipAddress);
                if (isOnvifCamera)
                {
                    // Add to Tapo devices list if ONVIF confirms it's a camera
                    await AddTapoCameraViaONVIF(ipAddress);
                    return true;
                }
            }
            
            // Try HTTPS discovery if port 443 is open
            if (await IsPortOpen(ipAddress, 443, 300))
            {
                var isTapoHTTPS = await CheckTapoHTTPS(ipAddress);
                if (isTapoHTTPS)
                {
                    await AddTapoCameraViaHTTPS(ipAddress);
                    return true;
                }
            }
        }

        return false;
    }

    static async Task<bool> CheckONVIFCapabilities(string ipAddress)
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
                return responseText.Contains("Media") || responseText.Contains("PTZ") || 
                       responseText.Contains("Analytics") || responseText.Contains("Imaging");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ONVIF check failed for {ipAddress}: {ex.Message}");
        }
        
        return false;
    }

    static async Task<bool> CheckTapoHTTPS(string ipAddress)
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
            return response.StatusCode == System.Net.HttpStatusCode.NotFound || 
                   response.Headers.Server?.ToString().Contains("TP-LINK", StringComparison.OrdinalIgnoreCase) == true;
        }
        catch (HttpRequestException)
        {
            // This might actually indicate a Tapo camera (they often reject generic HTTP requests)
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    static async Task AddTapoCameraViaONVIF(string ipAddress)
    {
        var device = new TapoDevice
        {
            IPAddress = ipAddress,
            DeviceType = "Tapo Camera (ONVIF)",
            DiscoveryMethod = "ONVIF Port Scan",
            Port = 2020
        };

        // Try to get more details from ONVIF if possible
        try
        {
            // Send a more detailed ONVIF query to get device info
            // This would require authentication, so we'll just mark it as discovered
            device.Model = "Unknown Tapo Model";
        }
        catch { }

        lock (lockObj)
        {
            if (!discoveredTapoDevices.Any(d => d.IPAddress == ipAddress))
            {
                discoveredTapoDevices.Add(device);
                Console.WriteLine($"✅ Found Tapo camera via ONVIF: {ipAddress}");
            }
        }
    }

    static async Task AddTapoCameraViaHTTPS(string ipAddress)
    {
        var device = new TapoDevice
        {
            IPAddress = ipAddress,
            DeviceType = "Tapo Camera (HTTPS)",
            DiscoveryMethod = "HTTPS Port Scan",
            Port = 443
        };

        // Try to determine camera model by checking additional ports
        if (await IsPortOpen(ipAddress, 8800, 200))
        {
            device.DeviceType = "Tapo Camera with Streaming";
        }
        
        if (await IsPortOpen(ipAddress, 554, 200))
        {
            device.DeviceType += " (RTSP)";
        }

        lock (lockObj)
        {
            if (!discoveredTapoDevices.Any(d => d.IPAddress == ipAddress))
            {
                discoveredTapoDevices.Add(device);
                Console.WriteLine($"✅ Found Tapo camera via HTTPS: {ipAddress}");
            }
        }
    }

    static async Task<List<int>> ScanCommonPorts(string ipAddress)
    {
        var openPorts = new List<int>();
        // Include Tapo-specific ports in the scan
        var commonPorts = new[] { 22, 23, 53, 80, 135, 139, 443, 445, 548, 554, 631, 993, 995, 1900, 2020, 3389, 5353, 8080, 8800, 9100 };

        var tasks = commonPorts.Select(async port =>
        {
            if (await IsPortOpen(ipAddress, port, 500))
            {
                lock (openPorts)
                {
                    openPorts.Add(port);
                }
            }
        });

        await Task.WhenAll(tasks);
        return openPorts.OrderBy(p => p).ToList();
    }

    static async Task<bool> IsPortOpen(string host, int port, int timeout)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(timeout);
            
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            
            if (completedTask == connectTask && client.Connected)
            {
                return true;
            }
        }
        catch
        {
            // Port is closed or host is unreachable
        }
        
        return false;
    }

    static void DisplayResults()
    {
        Console.WriteLine("\nNetwork Discovery Results:");
        Console.WriteLine("==========================");
        
        // Display Tapo devices first
        if (discoveredTapoDevices.Any())
        {
            Console.WriteLine($"\n🔌 TAPO DEVICES FOUND ({discoveredTapoDevices.Count}):");
            Console.WriteLine("═══════════════════════════════");
            
            foreach (var device in discoveredTapoDevices.OrderBy(d => IPAddress.Parse(d.IPAddress).GetAddressBytes()[3]))
            {
                Console.WriteLine($"📍 IP Address: {device.IPAddress}");
                Console.WriteLine($"   Device Type: {device.DeviceType ?? "Unknown"}");
                Console.WriteLine($"   Model: {device.Model ?? "Unknown"}");
                Console.WriteLine($"   Discovery Method: {device.DiscoveryMethod}");
                
                if (!string.IsNullOrEmpty(device.MacAddress))
                    Console.WriteLine($"   MAC Address: {device.MacAddress}");
                    
                if (!string.IsNullOrEmpty(device.DeviceId))
                    Console.WriteLine($"   Device ID: {device.DeviceId}");
                    
                if (!string.IsNullOrEmpty(device.Alias))
                    Console.WriteLine($"   Alias: {device.Alias}");
                    
                if (!string.IsNullOrEmpty(device.Nickname))
                    Console.WriteLine($"   Nickname: {device.Nickname}");
                    
                if (!string.IsNullOrEmpty(device.HardwareVersion))
                    Console.WriteLine($"   Hardware Version: {device.HardwareVersion}");
                    
                if (!string.IsNullOrEmpty(device.SoftwareVersion))
                    Console.WriteLine($"   Software Version: {device.SoftwareVersion}");
                
                if (device.Port > 0)
                {
                    if (device.ResponsePort > 0 && device.ResponsePort != device.Port)
                        Console.WriteLine($"   Discovery Port: {device.Port} | Response Port: {device.ResponsePort}");
                    else
                        Console.WriteLine($"   Port: {device.Port}");
                }
                    
                Console.WriteLine();
            }
        }
        
        // Display general network devices
        if (discoveredDevices.Any())
        {
            Console.WriteLine($"\n🌐 OTHER NETWORK DEVICES ({discoveredDevices.Count}):");
            Console.WriteLine("═══════════════════════════════════════");
            
            foreach (var device in discoveredDevices.OrderBy(d => IPAddress.Parse(d.IPAddress).GetAddressBytes()[3]))
            {
                Console.WriteLine($"📍 IP Address: {device.IPAddress}");
                Console.WriteLine($"   Hostname: {device.HostName}");
                Console.WriteLine($"   Device Type: {device.DeviceType}");
                Console.WriteLine($"   Response Time: {device.ResponseTime}ms");
                
                if (device.OpenPorts.Any())
                {
                    Console.WriteLine($"   Open Ports: {string.Join(", ", device.OpenPorts)}");
                }
                
                Console.WriteLine();
            }
        }

        // Summary
        var totalDevices = discoveredTapoDevices.Count + discoveredDevices.Count;
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine($"📊 DISCOVERY SUMMARY:");
        Console.WriteLine($"   • Tapo Devices: {discoveredTapoDevices.Count}");
        Console.WriteLine($"   • Other Devices: {discoveredDevices.Count}");
        Console.WriteLine($"   • Total Devices: {totalDevices}");
        
        if (totalDevices == 0)
        {
            Console.WriteLine("\n⚠️  No devices found on the network.");
            Console.WriteLine("   This could be due to:");
            Console.WriteLine("   • Firewall blocking discovery packets");
            Console.WriteLine("   • Devices not responding to discovery methods");
            Console.WriteLine("   • Network configuration preventing broadcast");
        }
    }
}

class NetworkDevice
{
    public string IPAddress { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public long ResponseTime { get; set; }
    public bool IsReachable { get; set; }
    public List<int> OpenPorts { get; set; } = new List<int>();
}

class TapoDevice
{
    public string IPAddress { get; set; } = string.Empty;
    public string? MacAddress { get; set; }
    public string? DeviceId { get; set; }
    public string? Model { get; set; }
    public string? DeviceType { get; set; }
    public string? HardwareVersion { get; set; }
    public string? SoftwareVersion { get; set; }
    public string? Alias { get; set; }
    public string? Nickname { get; set; }
    public string DiscoveryMethod { get; set; } = string.Empty;
    public int Port { get; set; }
    public int ResponsePort { get; set; }
    public string? RawResponse { get; set; }
}