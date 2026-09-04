using RtspClientSharp;
using RtspClientSharp.RawFrames.Video;
using RtspClientSharp.RawFrames;

namespace TapoMaui.Services;

public class RTSPVideoService : IDisposable
{
    private RtspClient? _rtspClient;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isConnected;
    private string _currentUrl = string.Empty;

    public event EventHandler<byte[]>? FrameReceived;
    public event EventHandler<string>? ConnectionStatusChanged;
    public event EventHandler<Exception>? ErrorOccurred;

    public bool IsConnected => _isConnected;
    public string CurrentUrl => _currentUrl;

    public async Task<bool> ConnectAsync(string rtspUrl, TimeSpan? timeout = null)
    {
        try
        {
            await DisconnectAsync();

            System.Diagnostics.Debug.WriteLine($"🔄 Connecting to RTSP stream: {MaskPassword(rtspUrl)}");
            ConnectionStatusChanged?.Invoke(this, "Connecting to RTSP stream...");

            var connectionParameters = new ConnectionParameters(new Uri(rtspUrl))
            {
                RtpTransport = RtpTransportProtocol.TCP, // TCP is more reliable for Tapo cameras
                ConnectTimeout = timeout ?? TimeSpan.FromSeconds(10),
                ReceiveTimeout = TimeSpan.FromSeconds(30)
            };

            _rtspClient = new RtspClient(connectionParameters);
            _cancellationTokenSource = new CancellationTokenSource();

            // Subscribe to frame events
            _rtspClient.FrameReceived += OnFrameReceived;

            // Connect to RTSP stream
            await _rtspClient.ConnectAsync(_cancellationTokenSource.Token);

            _isConnected = true;
            _currentUrl = rtspUrl;

            System.Diagnostics.Debug.WriteLine($"✅ Successfully connected to RTSP stream");
            ConnectionStatusChanged?.Invoke(this, "Connected - receiving video frames");

            // Start receiving frames
            _ = Task.Run(async () =>
            {
                try
                {
                    await _rtspClient.ReceiveAsync(_cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("RTSP receive operation was cancelled");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"RTSP receive error: {ex.Message}");
                    ErrorOccurred?.Invoke(this, ex);
                }
            });

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ RTSP connection failed: {ex.Message}");
            ErrorOccurred?.Invoke(this, ex);
            ConnectionStatusChanged?.Invoke(this, $"Connection failed: {ex.Message}");
            
            await DisconnectAsync();
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _isConnected = false;
            _currentUrl = string.Empty;

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            if (_rtspClient != null)
            {
                _rtspClient.FrameReceived -= OnFrameReceived;
                _rtspClient.Dispose();
                _rtspClient = null;
            }

            ConnectionStatusChanged?.Invoke(this, "Disconnected");
            System.Diagnostics.Debug.WriteLine($"🛑 RTSP stream disconnected");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Error during disconnect: {ex.Message}");
        }
    }

    private void OnFrameReceived(object? sender, RawFrame rawFrame)
    {
        try
        {
            if (rawFrame is RawVideoFrame videoFrame)
            {
                // Convert video frame to byte array for display
                var frameData = ConvertVideoFrameToBytes(videoFrame);
                if (frameData != null)
                {
                    FrameReceived?.Invoke(this, frameData);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Error processing frame: {ex.Message}");
        }
    }

    private byte[]? ConvertVideoFrameToBytes(RawVideoFrame videoFrame)
    {
        try
        {
            // For RtspClientSharp, we need to access the frame data differently
            if (videoFrame is RawH264Frame h264Frame)
            {
                return h264Frame.FrameSegment.Array;
            }
            else if (videoFrame is RawJpegFrame jpegFrame)
            {
                return jpegFrame.FrameSegment.Array;
            }
            else
            {
                // For other frame types, try to access the raw data
                System.Diagnostics.Debug.WriteLine($"Received video frame type: {videoFrame.GetType().Name}");
                
                // Use reflection to get frame data if direct access is not available
                var frameSegmentProperty = videoFrame.GetType().GetProperty("FrameSegment");
                if (frameSegmentProperty != null)
                {
                    var frameSegment = frameSegmentProperty.GetValue(videoFrame);
                    var arrayProperty = frameSegment?.GetType().GetProperty("Array");
                    if (arrayProperty != null)
                    {
                        return arrayProperty.GetValue(frameSegment) as byte[];
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error converting video frame: {ex.Message}");
        }

        return null;
    }

    public static string MaskPassword(string rtspUrl)
    {
        try
        {
            var uri = new Uri(rtspUrl);
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var userInfo = uri.UserInfo.Split(':');
                if (userInfo.Length == 2)
                {
                    var maskedPassword = new string('*', userInfo[1].Length);
                    return rtspUrl.Replace($":{userInfo[1]}@", $":{maskedPassword}@");
                }
            }
        }
        catch
        {
            // If URL parsing fails, return original with generic masking
        }

        return rtspUrl;
    }

    public void Dispose()
    {
        DisconnectAsync().Wait();
    }
}

/// <summary>
/// Connection status for RTSP streams
/// </summary>
public enum RTSPConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Receiving,
    Error
}

/// <summary>
/// Video frame data with metadata
/// </summary>
public class VideoFrameData
{
    public byte[] FrameBytes { get; set; } = Array.Empty<byte>();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int Width { get; set; }
    public int Height { get; set; }
    public string CodecType { get; set; } = string.Empty;
}