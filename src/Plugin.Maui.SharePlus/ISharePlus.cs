namespace Plugin.Maui.SharePlus;

/// <summary>
/// Share for Android and iOS with title, subject, MIME type, preview,
/// target app, and FileProvider-safe temporary files.
/// </summary>
public interface ISharePlus : IDisposable
{
    /// <summary>
    /// Always <c>true</c> on Android and iOS. <c>false</c> on the <c>net10.0</c>
    /// reference assembly.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Raised after a share attempt finishes.
    /// </summary>
    event EventHandler<ShareCompletedEventArgs>? ShareCompleted;

    /// <summary>
    /// Deletes leftover copies in the share cache when
    /// <see cref="SharePlusOptions.DeleteShareCacheOnStart"/> is enabled.
    /// Safe to call more than once.
    /// </summary>
    void Start();

    /// <summary>
    /// Whether the destination can be used on this device.
    /// </summary>
    bool CanShare(ShareTarget target = ShareTarget.Any);

    /// <summary>
    /// Shares text.
    /// </summary>
    Task<ShareResult> ShareTextAsync(string text, string? title = null, string? subject = null, string? mimeType = null, SharePreview? preview = null, ShareTarget target = ShareTarget.Any, string? targetApp = null, IReadOnlyList<string>? recipients = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shares text using a request object.
    /// </summary>
    Task<ShareResult> ShareTextAsync(ShareTextRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shares one local file.
    /// </summary>
    Task<ShareResult> ShareFileAsync(string filePath, string? title = null, string? subject = null, string? mimeType = null, SharePreview? preview = null, ShareTarget target = ShareTarget.Any, TemporaryFileHandling temporaryFileHandling = TemporaryFileHandling.CopyToShareCache, string? text = null, string? fileName = null, string? targetApp = null, IReadOnlyList<string>? recipients = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shares one local file using a request object.
    /// </summary>
    Task<ShareResult> ShareFileAsync(ShareFileRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shares one or more local file paths.
    /// </summary>
    Task<ShareResult> ShareFilesAsync(IEnumerable<string> filePaths, string? title = null, string? subject = null, string? mimeType = null, SharePreview? preview = null, ShareTarget target = ShareTarget.Any, TemporaryFileHandling temporaryFileHandling = TemporaryFileHandling.CopyToShareCache, string? text = null, string? targetApp = null, IReadOnlyList<string>? recipients = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shares one or more files using a request object.
    /// </summary>
    Task<ShareResult> ShareFilesAsync(ShareFilesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes copies left in the dedicated share cache root.
    /// </summary>
    void CleanupShareCache();
}
