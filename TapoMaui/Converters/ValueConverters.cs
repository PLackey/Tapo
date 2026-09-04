using System.Globalization;
using TapoMaui.Models;
using Color = TapoMaui.Models.Color;

namespace TapoMaui.Converters;

public class InvertedBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue ? !boolValue : false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool boolValue ? !boolValue : false;
    }
}

public class IsNotNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null && !string.IsNullOrEmpty(value.ToString());
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOn)
        {
            return isOn ? Colors.Green : Colors.Red;
        }
        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ColorToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
        {
            // Convert HSV to RGB
            return HsvToRgb(color.Hue, color.Saturation, 100);
        }
        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
    
    private static Microsoft.Maui.Graphics.Color HsvToRgb(double h, double s, double v)
    {
        h = h / 360.0;
        s = s / 100.0;
        v = v / 100.0;
        
        int i = (int)(h * 6);
        double f = h * 6 - i;
        double p = v * (1 - s);
        double q = v * (1 - f * s);
        double t = v * (1 - (1 - f) * s);

        return (i % 6) switch
        {
            0 => Microsoft.Maui.Graphics.Color.FromRgb((float)v, (float)t, (float)p),
            1 => Microsoft.Maui.Graphics.Color.FromRgb((float)q, (float)v, (float)p),
            2 => Microsoft.Maui.Graphics.Color.FromRgb((float)p, (float)v, (float)t),
            3 => Microsoft.Maui.Graphics.Color.FromRgb((float)p, (float)q, (float)v),
            4 => Microsoft.Maui.Graphics.Color.FromRgb((float)t, (float)p, (float)v),
            _ => Microsoft.Maui.Graphics.Color.FromRgb((float)v, (float)p, (float)q),
        };
    }
}

public class StreamButtonTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter?.ToString() == "loading" && value is bool isLoading && isLoading)
            return "Connecting...";
        
        return value is bool isStreaming && isStreaming ? "Stop Stream" : "Start Stream";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StreamButtonColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter?.ToString() == "loading" && value is bool isLoading && isLoading)
            return Colors.Orange;
            
        return value is bool isStreaming && isStreaming ? Colors.Red : Colors.Blue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StreamStatusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter?.ToString() == "loading")
        {
            return value is bool isLoading && isLoading ? "🔄 Connecting..." : "";
        }
        
        return value is bool isStreaming && isStreaming ? "🔴 Live" : "⚫ Offline";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StreamStatusColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter?.ToString() == "loading" && value is bool isLoading && isLoading)
            return Colors.Orange;
            
        return value is bool isStreaming && isStreaming ? Colors.Red : Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StreamButtonEnabledConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Button is enabled when not loading and not busy
        return !(value is bool isLoadingOrBusy && isLoadingOrBusy);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}