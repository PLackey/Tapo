using CommunityToolkit.Mvvm.ComponentModel;

namespace TapoMaui.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;
    
    [ObservableProperty]
    private string _title = string.Empty;
    
    [ObservableProperty]
    private string _errorMessage = string.Empty;
    
    protected virtual void ShowError(string message)
    {
        ErrorMessage = message;
        // In a real app, you might show a toast or alert
        System.Diagnostics.Debug.WriteLine($"Error: {message}");
    }
    
    protected virtual void ClearError()
    {
        ErrorMessage = string.Empty;
    }
}