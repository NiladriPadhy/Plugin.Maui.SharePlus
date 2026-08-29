namespace Plugin.Maui.SharePlus;

/// <summary>
/// Optional preview shown in the share sheet (title and thumbnail).
/// iOS uses <c>LPLinkMetadata</c> / <c>UIActivityItemSource</c>.
/// Android sets <c>EXTRA_TITLE</c>; a thumbnail file is included when the payload is an image.
/// </summary>
public sealed class SharePreview
{
    /// <summary>
    /// Title shown in the share sheet preview. Falls back to the request <c>Title</c>.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Local path to a thumbnail image.
    /// </summary>
    public string? ThumbnailFilePath { get; init; }

    /// <summary>
    /// In-memory thumbnail. Used when <see cref="ThumbnailFilePath"/> is not set.
    /// </summary>
    public byte[]? ThumbnailBytes { get; init; }

    /// <summary>
    /// MIME type for <see cref="ThumbnailBytes"/>. Default is <c>image/png</c>.
    /// </summary>
    public string ThumbnailMimeType { get; init; } = "image/png";
}
