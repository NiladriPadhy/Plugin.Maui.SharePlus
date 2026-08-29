namespace Plugin.Maui.SharePlus;

/// <summary>
/// Text share with title, subject, MIME type, preview, and target app.
/// </summary>
public sealed class ShareTextRequest
{
    /// <summary>
    /// Text to share.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Share sheet / chooser title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Email subject or equivalent extra.
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// MIME type. Default is <c>text/plain</c>.
    /// </summary>
    public string? MimeType { get; init; }

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
    /// Recipients for <see cref="ShareTarget.Email"/> and <see cref="ShareTarget.Messages"/>.
    /// </summary>
    public IReadOnlyList<string>? Recipients { get; init; }
}
