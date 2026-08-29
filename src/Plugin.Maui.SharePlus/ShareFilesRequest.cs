namespace Plugin.Maui.SharePlus;

/// <summary>
/// Multi-file share with title, subject, MIME type, preview, target app,
/// and temporary file handling.
/// </summary>
public sealed class ShareFilesRequest
{
    /// <summary>
    /// Files to share. At least one is required.
    /// </summary>
    public required IReadOnlyList<ShareFileItem> Files { get; init; }

    /// <summary>
    /// Share sheet / chooser title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Email subject or equivalent extra.
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// MIME type for the intent. Inferred from the files when omitted;
    /// mixed types become <c>*/*</c>.
    /// </summary>
    public string? MimeType { get; init; }

    /// <summary>
    /// Optional caption sent alongside the files.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Optional share-sheet preview.
    /// </summary>
    public SharePreview? Preview { get; init; }

    /// <summary>
    /// Destination app. Default is the system share sheet.
    /// </summary>
    public ShareTarget Target { get; init; } = ShareTarget.Any;

    /// <summary>
    /// Optional Android package name or iOS URL scheme that overrides <see cref="Target"/>.
    /// </summary>
    public string? TargetApp { get; init; }

    /// <summary>
    /// How files are copied into the FileProvider-safe share cache.
    /// </summary>
    public TemporaryFileHandling TemporaryFileHandling { get; init; } = TemporaryFileHandling.CopyToShareCache;

    /// <summary>
    /// Recipients for <see cref="ShareTarget.Email"/> and <see cref="ShareTarget.Messages"/>.
    /// </summary>
    public IReadOnlyList<string>? Recipients { get; init; }
}
