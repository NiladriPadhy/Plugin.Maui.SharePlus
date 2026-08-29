namespace Plugin.Maui.SharePlus.Tests;

public sealed class ShareMimeTypesTests
{
    [Theory]
    [InlineData("report.pdf", "application/pdf")]
    [InlineData("photo.JPG", "image/jpeg")]
    [InlineData("notes.txt", "text/plain")]
    [InlineData("unknown.xyz", "application/octet-stream")]
    public void Infer_uses_extension_when_mime_omitted(string name, string expected)
    {
        Assert.Equal(expected, ShareMimeTypes.Infer(null, name));
    }

    [Fact]
    public void Infer_prefers_explicit_mime()
    {
        Assert.Equal("image/png", ShareMimeTypes.Infer("image/png", "file.pdf"));
    }

    [Fact]
    public void Combine_returns_wildcard_for_mixed_types()
    {
        var files = new[]
        {
            new PreparedShareFile { OriginalPath = "a", SharePath = "a", FileName = "a.txt", MimeType = "text/plain" },
            new PreparedShareFile { OriginalPath = "b", SharePath = "b", FileName = "b.pdf", MimeType = "application/pdf" }
        };

        Assert.Equal("*/*", ShareMimeTypes.Combine(files, null));
        Assert.Equal("application/zip", ShareMimeTypes.Combine(files, "application/zip"));
    }
}
