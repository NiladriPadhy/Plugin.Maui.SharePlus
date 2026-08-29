using Microsoft.Extensions.Logging;
using Plugin.Maui.SharePlus;

namespace Plugin.Maui.SharePlus.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<MainPage>();

        builder
            .UseMauiApp<App>()
            .UseMauiSharePlus(options =>
            {
                options.DefaultTitle = "SharePlus";
                options.DefaultTemporaryFileHandling = TemporaryFileHandling.CopyToShareCache;
                options.DeleteShareCacheOnStart = true;
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
