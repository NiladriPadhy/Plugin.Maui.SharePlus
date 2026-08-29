namespace Plugin.Maui.SharePlus;

/// <summary>
/// Registers SharePlus services without MAUI lifecycle hooks.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="ISharePlus"/> using the supplied options instance.
    /// </summary>
    public static IServiceCollection AddMauiSharePlus(this IServiceCollection services, SharePlusOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(options);
        services.TryAddSingleton<ISharePlus>(sp =>
        {
            var resolved = sp.GetService<SharePlusOptions>() ?? options;
            var share = SharePlus.Create(resolved);
            SharePlus.SetDefault(share);
            return share;
        });

        return services;
    }

    /// <summary>
    /// Adds <see cref="ISharePlus"/> and applies <paramref name="configure"/> to a new options instance.
    /// </summary>
    public static IServiceCollection AddMauiSharePlus(this IServiceCollection services, Action<SharePlusOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SharePlusOptions();
        configure?.Invoke(options);
        return services.AddMauiSharePlus(options);
    }
}
