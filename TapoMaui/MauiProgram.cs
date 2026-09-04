using Microsoft.Extensions.Logging;
using TapoMaui.Services;
using TapoMaui.ViewModels;
using TapoMaui.Views;
using TapoMaui.Converters;
using CommunityToolkit.Maui;

namespace TapoMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkitMediaElement()
            .ConfigureFonts(fonts =>
            {
                // Using system default fonts
            });

        // Register services
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<ITapoApiClient>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            // Credentials will be set dynamically from the UI
            return new TapoApiClient(httpClient);
        });

        // Register ViewModels
        builder.Services.AddTransient<MainViewModel>();

        // Register Views
        builder.Services.AddTransient<MainPage>();

        // Register Value Converters
        builder.Services.AddSingleton<InvertedBoolConverter>();
        builder.Services.AddSingleton<IsNotNullConverter>();
        builder.Services.AddSingleton<BoolToColorConverter>();
        builder.Services.AddSingleton<ColorToColorConverter>();
        builder.Services.AddSingleton<StreamButtonTextConverter>();
        builder.Services.AddSingleton<StreamButtonColorConverter>();
        builder.Services.AddSingleton<StreamStatusConverter>();
        builder.Services.AddSingleton<StreamStatusColorConverter>();
        builder.Services.AddSingleton<StreamButtonEnabledConverter>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}