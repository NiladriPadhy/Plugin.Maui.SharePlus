using Microsoft.Maui.Hosting;

namespace Plugin.Maui.SharePlus;

/// <summary>
/// MAUI host registration for SharePlus.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="ISharePlus"/> as a singleton and starts share-cache cleanup.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.UseMauiSharePlus(options =>
    /// {
    ///     options.DefaultTitle = "Share";
    ///     options.DefaultTemporaryFileHandling = TemporaryFileHandling.CopyToShareCache;
    /// });
    /// </code>
    /// </example>
    public static MauiAppBuilder UseMauiSharePlus(this MauiAppBuilder builder, Action<SharePlusOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new SharePlusOptions();
        configure?.Invoke(options);

        builder.Services.AddMauiSharePlus(options);
        builder.Services.AddTransient<IMauiInitializeService, SharePlusInitializer>();
        return builder;
    }
}
