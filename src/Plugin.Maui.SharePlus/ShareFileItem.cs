namespace Plugin.Maui.SharePlus;

/// <summary>
/// A local file to share.
/// </summary>
public sealed class ShareFileItem
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
    /// MIME type. Inferred from the extension when omitted.
    /// </summary>
    public string? MimeType { get; init; }
}
