namespace Plugin.Maui.SharePlus;

/// <summary>
/// Single-file share with title, subject, MIME type, preview, target app,
/// and temporary file handling.
/// </summary>
public sealed class ShareFileRequest
{
    /// <summary>
    /// Absolute path of the file on disk.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Display name presented to the receiving app. Defaults to the file name.
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// Share sheet / chooser title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Email subject or equivalent extra.
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// MIME type. Inferred from the extension when omitted.
    /// </summary>
    public string? MimeType { get; init; }

    /// <summary>
    /// Optional caption sent alongside the file (WhatsApp / Messages / Email body).
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
    /// How the file is copied into the FileProvider-safe share cache.
    /// </summary>
    public TemporaryFileHandling TemporaryFileHandling { get; init; } = TemporaryFileHandling.CopyToShareCache;

    /// <summary>
    /// Recipients for <see cref="ShareTarget.Email"/> and <see cref="ShareTarget.Messages"/>.
    /// </summary>
    public IReadOnlyList<string>? Recipients { get; init; }
}
