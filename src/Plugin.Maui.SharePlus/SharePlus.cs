namespace Plugin.Maui.SharePlus;

/// <summary>
/// Entry point for SharePlus when dependency injection is not used.
/// </summary>
public static class SharePlus
{
    static ISharePlus? _current;

    /// <summary>
    /// Gets the shared <see cref="ISharePlus"/> instance.
    /// </summary>
    public static ISharePlus Current => _current ??= Create(new SharePlusOptions());

    /// <summary>
    /// Always <c>true</c> on Android and iOS.
    /// </summary>
    public static bool IsSupported => Current.IsSupported;

    /// <summary>
    /// Raised after a share attempt finishes.
    /// </summary>
    public static event EventHandler<ShareCompletedEventArgs>? ShareCompleted
    {
        add => Current.ShareCompleted += value;
        remove => Current.ShareCompleted -= value;
    }

    /// <summary>
    /// Whether the destination can be used on this device.
    /// </summary>
    public static bool CanShare(ShareTarget target = ShareTarget.Any) => Current.CanShare(target);

    /// <summary>
    /// Shares text.
    /// </summary>
    /// <example>
    /// <code>
    /// await SharePlus.ShareTextAsync("Hello", title: "SharePlus", subject: "Note");
    /// </code>
    /// </example>
    public static Task<ShareResult> ShareTextAsync(string text, string? title = null, string? subject = null, string? mimeType = null, SharePreview? preview = null, ShareTarget target = ShareTarget.Any, string? targetApp = null, IReadOnlyList<string>? recipients = null, CancellationToken cancellationToken = default) =>
        Current.ShareTextAsync(text, title, subject, mimeType, preview, target, targetApp, recipients, cancellationToken);

    /// <summary>
    /// Shares text using a request object.
    /// </summary>
    public static Task<ShareResult> ShareTextAsync(ShareTextRequest request, CancellationToken cancellationToken = default) =>
        Current.ShareTextAsync(request, cancellationToken);

    /// <summary>
    /// Shares one local file.
    /// </summary>
    /// <example>
    /// <code>
    /// await SharePlus.ShareFileAsync(path, title: "Invoice", mimeType: "application/pdf", target: ShareTarget.Email);
    /// </code>
    /// </example>
    public static Task<ShareResult> ShareFileAsync(string filePath, string? title = null, string? subject = null, string? mimeType = null, SharePreview? preview = null, ShareTarget target = ShareTarget.Any, TemporaryFileHandling temporaryFileHandling = TemporaryFileHandling.CopyToShareCache, string? text = null, string? fileName = null, string? targetApp = null, IReadOnlyList<string>? recipients = null, CancellationToken cancellationToken = default) =>
        Current.ShareFileAsync(filePath, title, subject, mimeType, preview, target, temporaryFileHandling, text, fileName, targetApp, recipients, cancellationToken);

    /// <summary>
    /// Shares one local file using a request object.
    /// </summary>
    public static Task<ShareResult> ShareFileAsync(ShareFileRequest request, CancellationToken cancellationToken = default) =>
        Current.ShareFileAsync(request, cancellationToken);

    /// <summary>
    /// Shares one or more local file paths.
    /// </summary>
    public static Task<ShareResult> ShareFilesAsync(IEnumerable<string> filePaths, string? title = null, string? subject = null, string? mimeType = null, SharePreview? preview = null, ShareTarget target = ShareTarget.Any, TemporaryFileHandling temporaryFileHandling = TemporaryFileHandling.CopyToShareCache, string? text = null, string? targetApp = null, IReadOnlyList<string>? recipients = null, CancellationToken cancellationToken = default) =>
        Current.ShareFilesAsync(filePaths, title, subject, mimeType, preview, target, temporaryFileHandling, text, targetApp, recipients, cancellationToken);

    /// <summary>
    /// Shares one or more files using a request object.
    /// </summary>
    public static Task<ShareResult> ShareFilesAsync(ShareFilesRequest request, CancellationToken cancellationToken = default) =>
        Current.ShareFilesAsync(request, cancellationToken);

    /// <summary>
    /// Deletes copies left in the dedicated share cache root.
    /// </summary>
    public static void CleanupShareCache() => Current.CleanupShareCache();

    /// <summary>
    /// Creates a share client for the current platform.
    /// </summary>
    public static ISharePlus Create(SharePlusOptions? options = null)
    {
        options ??= new SharePlusOptions();
        return new SharePlusImplementation(options, CreatePlatform(), new ShareFilePreparer(options));
    }

    /// <summary>
    /// Replaces the shared instance. Intended for tests and custom implementations.
    /// </summary>
    public static void SetDefault(ISharePlus implementation) =>
        _current = implementation ?? throw new ArgumentNullException(nameof(implementation));

    internal static SharePlusImplementation Create(
        SharePlusOptions options,
        ISharePlatform platform,
        IShareFilePreparer? preparer = null) =>
        new(options, platform, preparer ?? new ShareFilePreparer(options));

    static ISharePlatform CreatePlatform()
    {
#if ANDROID
        return new AndroidSharePlatform();
#elif IOS
        return new IosSharePlatform();
#else
        return new UnsupportedSharePlatform();
#endif
    }
}
