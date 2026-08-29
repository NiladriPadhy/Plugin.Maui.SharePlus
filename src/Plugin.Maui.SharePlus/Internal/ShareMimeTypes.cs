namespace Plugin.Maui.SharePlus;

static class ShareMimeTypes
{
    public const string TextPlain = "text/plain";
    public const string Any = "*/*";

    public static string Infer(string? mimeType, string filePathOrName)
    {
        if (!string.IsNullOrWhiteSpace(mimeType))
            return mimeType;

        var extension = Path.GetExtension(filePathOrName);
        return InferFromExtension(extension);
    }

    public static string InferFromExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return "application/octet-stream";

        return extension.Trim().ToLowerInvariant() switch
        {
            ".txt" => TextPlain,
            ".html" or ".htm" => "text/html",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            ".mp4" => "video/mp4",
            ".mov" => "video/quicktime",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream"
        };
    }

    public static string Combine(IReadOnlyList<PreparedShareFile> files, string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested))
            return requested;
        if (files.Count == 0)
            return Any;

        var first = files[0].MimeType;
        for (var i = 1; i < files.Count; i++)
        {
            if (!string.Equals(files[i].MimeType, first, StringComparison.OrdinalIgnoreCase))
                return Any;
        }

        return first;
    }

    public static string ExtensionForThumbnail(string mimeType) =>
        mimeType.ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".png"
        };
}
