using TapoMaui.Services;

namespace TapoMaui.Views;

public partial class RTSPVideoViewer : ContentView
{
    private RTSPVideoService? _rtspService;
    private string _rtspUrl = string.Empty;
    private DateTime _lastFrameTime = DateTime.MinValue;
    private int _frameCount = 0;
    private DateTime _fpsStartTime = DateTime.UtcNow;

    public RTSPVideoViewer()
    {
        InitializeComponent();
        
        // Initialize RTSP service
        _rtspService = new RTSPVideoService();
        _rtspService.FrameReceived += OnFrameReceived;
        _rtspService.ConnectionStatusChanged += OnConnectionStatusChanged;
        _rtspService.ErrorOccurred += OnErrorOccurred;
    }

    public async Task ConnectToStreamAsync(string rtspUrl)
    {
        _rtspUrl = rtspUrl;
        
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            LoadingOverlay.IsVisible = true;
            ErrorOverlay.IsVisible = false;
            InfoOverlay.IsVisible = false;
            StatusLabel.Text = "Connecting to RTSP stream...";
            ConnectButton.IsEnabled = false;
        });

        if (_rtspService != null)
        {
            var success = await _rtspService.ConnectAsync(rtspUrl, TimeSpan.FromSeconds(15));
            
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (success)
                {
                    LoadingOverlay.IsVisible = false;
                    InfoOverlay.IsVisible = true;
                    DisconnectButton.IsEnabled = true;
                    FullscreenButton.IsEnabled = true;
                    
                    _fpsStartTime = DateTime.UtcNow;
                    _frameCount = 0;
                }
                else
                {
                    ShowError("Failed to connect to RTSP stream");
                }
                
                ConnectButton.IsEnabled = true;
            });
        }
    }

    public async Task DisconnectAsync()
    {
        if (_rtspService != null)
        {
            await _rtspService.DisconnectAsync();
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            LoadingOverlay.IsVisible = false;
            ErrorOverlay.IsVisible = false;
            InfoOverlay.IsVisible = false;
            
            VideoImage.Source = null;
            
            ConnectButton.IsEnabled = true;
            DisconnectButton.IsEnabled = false;
            FullscreenButton.IsEnabled = false;
            
            FpsLabel.Text = "0 FPS";
        });
    }

    private void OnFrameReceived(object? sender, byte[] frameData)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Convert byte array to ImageSource
                    var imageSource = ImageSource.FromStream(() => new MemoryStream(frameData));
                    VideoImage.Source = imageSource;

                    // Update FPS counter
                    UpdateFpsCounter();

                    // Ensure overlays are in correct state
                    if (LoadingOverlay.IsVisible)
                    {
                        LoadingOverlay.IsVisible = false;
                        InfoOverlay.IsVisible = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error displaying frame: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in OnFrameReceived: {ex.Message}");
        }
    }

    private void OnConnectionStatusChanged(object? sender, string status)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text = status;
            System.Diagnostics.Debug.WriteLine($"🔄 RTSP Status: {status}");
        });
    }

    private void OnErrorOccurred(object? sender, Exception error)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ShowError($"RTSP Error: {error.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ RTSP Error: {error.Message}");
        });
    }

    private void ShowError(string message)
    {
        LoadingOverlay.IsVisible = false;
        InfoOverlay.IsVisible = false;
        ErrorOverlay.IsVisible = true;
        ErrorLabel.Text = message;
        
        ConnectButton.IsEnabled = true;
        DisconnectButton.IsEnabled = false;
        FullscreenButton.IsEnabled = false;
    }

    private void UpdateFpsCounter()
    {
        _frameCount++;
        var elapsed = DateTime.UtcNow - _fpsStartTime;
        
        if (elapsed.TotalSeconds >= 1.0)
        {
            var fps = _frameCount / elapsed.TotalSeconds;
            FpsLabel.Text = $"{fps:F1} FPS";
            
            _frameCount = 0;
            _fpsStartTime = DateTime.UtcNow;
        }
    }

    private async void OnConnectClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_rtspUrl))
        {
            await ConnectToStreamAsync(_rtspUrl);
        }
    }

    private async void OnDisconnectClicked(object sender, EventArgs e)
    {
        await DisconnectAsync();
    }

    private async void OnRetryClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_rtspUrl))
        {
            await ConnectToStreamAsync(_rtspUrl);
        }
    }

    private void OnFullscreenClicked(object sender, EventArgs e)
    {
        // For future implementation - full screen video view
        _ = DisplayAlertAsync("Fullscreen", "Fullscreen mode not yet implemented", "OK");
    }

    private async Task DisplayAlertAsync(string title, string message, string cancel)
    {
        var page = GetParentPage();
        if (page != null)
        {
            await page.DisplayAlert(title, message, cancel);
        }
    }

    private Page? GetParentPage()
    {
        Element? element = this;
        while (element != null)
        {
            if (element is Page page)
                return page;
            element = element.Parent;
        }
        return null;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        
        // Clean up when control is removed
        if (Handler == null)
        {
            _rtspService?.DisconnectAsync();
        }
    }

    ~RTSPVideoViewer()
    {
        _rtspService?.Dispose();
    }
}