using TapoMaui.ViewModels;

namespace TapoMaui.Views;

public partial class DeviceItemView : ContentView
{
    public DeviceItemView()
    {
        InitializeComponent();
    }
    
    private async void OnBrightnessChanged(object sender, ValueChangedEventArgs e)
    {
        if (BindingContext is DeviceItemViewModel viewModel)
        {
            var brightness = (int)Math.Round(e.NewValue);
            await viewModel.SetBrightnessCommand.ExecuteAsync(brightness);
        }
    }
    
    private async void OnColorTemperatureChanged(object sender, ValueChangedEventArgs e)
    {
        if (BindingContext is DeviceItemViewModel viewModel)
        {
            var temperature = (int)Math.Round(e.NewValue);
            await viewModel.SetColorTemperatureCommand.ExecuteAsync(temperature);
        }
    }
}