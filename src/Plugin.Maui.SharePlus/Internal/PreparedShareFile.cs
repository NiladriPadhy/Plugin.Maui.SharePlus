namespace Plugin.Maui.SharePlus;

sealed class PreparedShareFile
{
    public required string OriginalPath { get; init; }

    public required string SharePath { get; init; }

    public required string FileName { get; init; }

    public required string MimeType { get; init; }

    public bool IsTemporary { get; init; }
}
