using TapoMaui.ViewModels;

namespace TapoMaui.Views;

public partial class VideoPlayerPage : ContentPage
{
    private DeviceItemViewModel? _viewModel;
    private bool _useNativeRTSP = true;

    public VideoPlayerPage(DeviceItemViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        
        // Subscribe to streaming status changes
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DeviceItemViewModel.IsStreaming) && _viewModel != null)
        {
            if (_viewModel.IsStreaming && _useNativeRTSP && !string.IsNullOrEmpty(_viewModel.StreamUrl))
            {
                // Start native RTSP streaming
                await RtspViewer.ConnectToStreamAsync(_viewModel.StreamUrl);
            }
            else if (!_viewModel.IsStreaming)
            {
                // Stop native RTSP streaming
                await RtspViewer.DisconnectAsync();
            }
        }
    }

    private async void OnNativeRTSPClicked(object sender, EventArgs e)
    {
        if (_viewModel != null && !string.IsNullOrEmpty(_viewModel.StreamUrl))
        {
            await RtspViewer.ConnectToStreamAsync(_viewModel.StreamUrl);
        }
        else
        {
            await DisplayAlertAsync("No Stream", "Start streaming first to use native RTSP player.", "OK");
        }
    }

    private void OnPlayerModeChanged(object sender, ToggledEventArgs e)
    {
        _useNativeRTSP = e.Value;
        
        if (_useNativeRTSP)
        {
            RtspViewer.IsVisible = true;
            MediaElementContainer.IsVisible = false;
            PlayerModeLabel.Text = "Native RTSP Client";
            PlayerModeLabel.TextColor = Colors.Green;
        }
        else
        {
            RtspViewer.IsVisible = false;
            MediaElementContainer.IsVisible = true;
            PlayerModeLabel.Text = "MediaElement (Limited)";
            PlayerModeLabel.TextColor = Colors.Orange;
        }
    }

    private async void OnOpenVLCClicked(object sender, EventArgs e)
    {
        if (_viewModel == null) return;

        try
        {
            var streamUrl = _viewModel.StreamUrl;
            if (string.IsNullOrEmpty(streamUrl))
            {
                await DisplayAlertAsync("Error", "No stream URL available. Start streaming first.", "OK");
                return;
            }

            // Try to copy URL to clipboard
            await Clipboard.SetTextAsync(streamUrl);
            
            // Show instructions to user
            var message = $"📹 RTSP URL copied to clipboard!\n\n" +
                         $"🎥 To view the camera stream:\n\n" +
                         $"1️⃣ Open VLC Media Player\n" +
                         $"2️⃣ Go to Media → Open Network Stream\n" +
                         $"3️⃣ Paste URL from clipboard (Ctrl+V)\n" +
                         $"4️⃣ Click Play\n\n" +
                         $"📱 Alternative apps:\n" +
                         $"• Any RTSP-compatible video player\n" +
                         $"• IP Camera Viewer apps\n" +
                         $"• Web browsers (some support RTSP)\n\n" +
                         $"🔗 Stream URL:\n" +
                         $"{Services.RTSPVideoService.MaskPassword(streamUrl)}";

            await DisplayAlertAsync("📹 Open Camera Stream", message, "Got it!");
            
            // Try to launch VLC if possible (platform specific)
            try
            {
                // Try VLC protocol handler
                await Launcher.OpenAsync(new Uri($"vlc://{streamUrl}"));
            }
            catch
            {
                try
                {
                    // Try generic video protocol
                    await Launcher.OpenAsync(new Uri(streamUrl));
                }
                catch
                {
                    // Protocol handlers not available, clipboard copy is enough
                    System.Diagnostics.Debug.WriteLine("No protocol handlers available for RTSP URLs");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to copy URL: {ex.Message}", "OK");
        }
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        // Stop native RTSP streaming
        await RtspViewer.DisconnectAsync();
        
        // Stop streaming when closing the page
        if (BindingContext is DeviceItemViewModel viewModel && viewModel.IsStreaming)
        {
            viewModel.StopStreamCommand.Execute(null);
        }
        
        await Navigation.PopAsync();
    }

    protected override async void OnDisappearing()
    {
        // Stop native RTSP streaming
        await RtspViewer.DisconnectAsync();
        
        // Stop streaming when navigating away
        if (BindingContext is DeviceItemViewModel viewModel && viewModel.IsStreaming)
        {
            viewModel.StopStreamCommand.Execute(null);
        }
        
        base.OnDisappearing();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        
        // Clean up when page is removed
        if (Handler == null && _viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }
}