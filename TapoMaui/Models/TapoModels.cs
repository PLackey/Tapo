using System.Text.Json.Serialization;

namespace TapoMaui.Models;

public enum DeviceType
{
    Light,
    ColorLight,
    RgbLightStrip,
    RgbicLightStrip,
    Plug,
    PlugEnergyMonitoring,
    PowerStrip,
    PowerStripEnergyMonitoring,
    Hub,
    Camera,
    CameraPtz,
    Switch,
    Sensor,
    Unknown
}

public class DeviceInfo
{
    [JsonPropertyName("device_id")]
    public string DeviceId { get; set; } = string.Empty;
    
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("device_on")]
    public bool DeviceOn { get; set; }
    
    [JsonPropertyName("on_time")]
    public int OnTime { get; set; }
    
    [JsonPropertyName("brightness")]
    public int? Brightness { get; set; }
    
    [JsonPropertyName("color_temp")]
    public int? ColorTemp { get; set; }
    
    [JsonPropertyName("hue")]
    public int? Hue { get; set; }
    
    [JsonPropertyName("saturation")]
    public int? Saturation { get; set; }
    
    [JsonPropertyName("ip")]
    public string IpAddress { get; set; } = string.Empty;
    
    // Additional properties for enhanced discovery
    [JsonPropertyName("mac")]
    public string? MacAddress { get; set; }
    
    [JsonPropertyName("alias")]
    public string? Alias { get; set; }
    
    [JsonPropertyName("hw_ver")]
    public string? HardwareVersion { get; set; }
    
    [JsonPropertyName("sw_ver")]
    public string? SoftwareVersion { get; set; }
}

public class DiscoveredDevice
{
    public DeviceInfo DeviceInfo { get; set; } = new();
    public DeviceType DeviceType { get; set; }
    public string IpAddress { get; set; } = string.Empty;
}

public class EnergyUsage
{
    [JsonPropertyName("today_runtime")]
    public int TodayRuntime { get; set; }
    
    [JsonPropertyName("month_runtime")]
    public int MonthRuntime { get; set; }
    
    [JsonPropertyName("today_energy")]
    public int TodayEnergy { get; set; }
    
    [JsonPropertyName("month_energy")]
    public int MonthEnergy { get; set; }
}

public class CurrentPower
{
    [JsonPropertyName("current_power")]
    public int Power { get; set; }
}

public class Color
{
    public static readonly Color Chocolate = new() { Name = "Chocolate", Hue = 25, Saturation = 75 };
    public static readonly Color HotPink = new() { Name = "Hot Pink", Hue = 330, Saturation = 100 };
    public static readonly Color DeepSkyBlue = new() { Name = "Deep Sky Blue", Hue = 195, Saturation = 100 };
    public static readonly Color Red = new() { Name = "Red", Hue = 0, Saturation = 100 };
    public static readonly Color Green = new() { Name = "Green", Hue = 120, Saturation = 100 };
    public static readonly Color Blue = new() { Name = "Blue", Hue = 240, Saturation = 100 };
    public static readonly Color Yellow = new() { Name = "Yellow", Hue = 60, Saturation = 100 };
    public static readonly Color Purple = new() { Name = "Purple", Hue = 270, Saturation = 100 };
    public static readonly Color Orange = new() { Name = "Orange", Hue = 30, Saturation = 100 };
    
    public string Name { get; set; } = string.Empty;
    public int Hue { get; set; }
    public int Saturation { get; set; }
    
    public static List<Color> PredefinedColors { get; } = GetPredefinedColors();
    
    public static List<Color> GetPredefinedColors()
    {
        return new List<Color>
        {
            Red, Green, Blue, Yellow, Purple, Orange,
            Chocolate, HotPink, DeepSkyBlue
        };
    }
}

public class TapoRequest
{
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;
    
    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

public class TapoResponse<T>
{
    [JsonPropertyName("error_code")]
    public int ErrorCode { get; set; }
    
    [JsonPropertyName("result")]
    public T? Result { get; set; }
}

public class SetDeviceInfoParams
{
    [JsonPropertyName("device_on")]
    public bool? DeviceOn { get; set; }
    
    [JsonPropertyName("brightness")]
    public int? Brightness { get; set; }
    
    [JsonPropertyName("color_temp")]
    public int? ColorTemp { get; set; }
    
    [JsonPropertyName("hue")]
    public int? Hue { get; set; }
    
    [JsonPropertyName("saturation")]
    public int? Saturation { get; set; }
}