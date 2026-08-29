namespace Plugin.Maui.SharePlus.Tests;

public sealed class SharePlusTests
{
    [Fact]
    public async Task ShareTextAsync_passes_title_subject_mime_preview_and_target()
    {
        var (share, platform, _, _) = Harness.Create();
        var preview = new SharePreview { Title = "Preview title" };

        var result = await share.ShareTextAsync(
            "hello from SharePlus",
            title: "Chooser",
            subject: "Subject line",
            mimeType: "text/html",
            preview: preview,
            target: ShareTarget.Email,
            recipients: ["dev@example.com"]);

        Assert.True(result.Completed);
        Assert.Equal(ShareKind.Text, result.Kind);
        Assert.Equal(ShareTarget.Email, platform.LastResolved);
        Assert.Equal("hello from SharePlus", platform.LastText?.Text);
        Assert.Equal("Chooser", platform.LastText?.Title);
        Assert.Equal("Subject line", platform.LastText?.Subject);
        Assert.Equal("text/html", platform.LastText?.MimeType);
        Assert.Same(preview, platform.LastText?.Preview);
        Assert.Equal(["dev@example.com"], platform.LastText?.Recipients);
    }

    [Fact]
    public async Task ShareTextAsync_uses_default_title_and_text_plain()
    {
        var (share, platform, _, _) = Harness.Create();

        await share.ShareTextAsync("payload");

        Assert.Equal("SharePlus", platform.LastText?.Title);
        Assert.Equal("text/plain", platform.LastText?.MimeType);
        Assert.Equal(ShareTarget.Any, platform.LastResolved);
    }

    [Fact]
    public async Task ShareTextAsync_rejects_empty_text()
    {
        var (share, _, _, _) = Harness.Create();

        var error = await Assert.ThrowsAsync<SharePlusException>(() => share.ShareTextAsync("   "));
        Assert.Equal(SharePlusError.InvalidRequest, error.Error);
    }

    [Fact]
    public async Task ShareFileAsync_copies_into_share_cache_and_infers_mime()
    {
        var (share, platform, _, root) = Harness.Create();
        var source = Harness.WriteTempFile(name: "invoice.pdf", contents: "%PDF");

        var result = await share.ShareFileAsync(
            source,
            title: "Invoice",
            subject: "Q3",
            preview: new SharePreview { Title = "Invoice preview" },
            target: ShareTarget.WhatsApp,
            temporaryFileHandling: TemporaryFileHandling.CopyToShareCache,
            text: "Please review");

        Assert.True(result.Completed);
        Assert.Equal(ShareKind.File, result.Kind);
        Assert.NotNull(platform.LastPrepared);
        var prepared = platform.LastPrepared![0];
        Assert.Equal("invoice.pdf", prepared.FileName);
        Assert.Equal("application/pdf", prepared.MimeType);
        Assert.StartsWith(root, prepared.SharePath, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(source, prepared.SharePath);
        Assert.True(File.Exists(prepared.SharePath));
        Assert.Equal("Invoice", platform.LastFiles?.Title);
        Assert.Equal("Q3", platform.LastFiles?.Subject);
        Assert.Equal("Please review", platform.LastFiles?.Text);
        Assert.Equal(ShareTarget.WhatsApp, platform.LastResolved);
    }

    [Fact]
    public async Task ShareFilesAsync_shares_multiple_files()
    {
        var (share, platform, _, _) = Harness.Create();
        var first = Harness.WriteTempFile(name: "a.txt");
        var second = Harness.WriteTempFile(name: "b.png", contents: "png");

        var result = await share.ShareFilesAsync([first, second], title: "Bundle", mimeType: "*/*");

        Assert.Equal(ShareKind.Files, result.Kind);
        Assert.Equal(2, platform.LastPrepared?.Count);
        Assert.Equal("*/*", platform.LastFiles?.MimeType);
    }

    [Fact]
    public async Task ShareFileAsync_missing_file_throws()
    {
        var (share, _, _, _) = Harness.Create();

        var error = await Assert.ThrowsAsync<SharePlusException>(
            () => share.ShareFileAsync(Path.Combine(Path.GetTempPath(), "missing-shareplus.txt")));
        Assert.Equal(SharePlusError.FileNotFound, error.Error);
    }

    [Fact]
    public async Task UseOriginal_outside_root_throws()
    {
        var (share, _, _, _) = Harness.Create();
        var source = Harness.WriteTempFile();

        var error = await Assert.ThrowsAsync<SharePlusException>(() =>
            share.ShareFileAsync(source, temporaryFileHandling: TemporaryFileHandling.UseOriginal));
        Assert.Equal(SharePlusError.FileOutsideShareRoot, error.Error);
    }

    [Fact]
    public async Task PreferOriginal_uses_file_already_in_share_root()
    {
        var (share, platform, _, root) = Harness.Create();
        var source = Harness.WriteTempFile(root, "inside.txt", "cached");

        await share.ShareFileAsync(source, temporaryFileHandling: TemporaryFileHandling.PreferOriginal);

        Assert.Equal(Path.GetFullPath(source), platform.LastPrepared![0].SharePath);
        Assert.False(platform.LastPrepared[0].IsTemporary);
    }

    [Fact]
    public async Task CopyAndDeleteAfterShare_marks_temporary_and_deletes()
    {
        var (share, platform, _, _) = Harness.Create();
        var source = Harness.WriteTempFile();

        await share.ShareFileAsync(source, temporaryFileHandling: TemporaryFileHandling.CopyAndDeleteAfterShare);

        Assert.True(platform.LastPrepared![0].IsTemporary);
        Assert.False(File.Exists(platform.LastPrepared[0].SharePath));
    }

    [Fact]
    public async Task Target_unavailable_returns_result()
    {
        var (share, platform, _, _) = Harness.Create();
        platform.AvailableTargets.Remove(ShareTarget.WhatsApp);

        var result = await share.ShareTextAsync("hi", target: ShareTarget.WhatsApp);

        Assert.Equal(ShareStatus.TargetUnavailable, result.Status);
        Assert.Null(platform.LastText);
    }

    [Fact]
    public async Task Target_unavailable_can_throw()
    {
        var (share, platform, _, _) = Harness.Create(options => options.ThrowWhenTargetUnavailable = true);
        platform.AvailableTargets.Remove(ShareTarget.Email);

        var error = await Assert.ThrowsAsync<SharePlusException>(
            () => share.ShareTextAsync("hi", target: ShareTarget.Email));
        Assert.Equal(SharePlusError.TargetUnavailable, error.Error);
    }

    [Fact]
    public async Task Target_unavailable_can_fall_back_to_share_sheet()
    {
        var (share, platform, _, _) = Harness.Create(options => options.FallbackToShareSheetWhenTargetUnavailable = true);
        platform.AvailableTargets.Remove(ShareTarget.NearbyShare);

        await share.ShareTextAsync("hi", target: ShareTarget.NearbyShare);

        Assert.Equal(ShareTarget.Any, platform.LastResolved);
        Assert.NotNull(platform.LastText);
    }

    [Fact]
    public async Task TargetApp_skips_enum_availability_check()
    {
        var (share, platform, _, _) = Harness.Create();
        platform.AvailableTargets.Remove(ShareTarget.WhatsApp);

        await share.ShareTextAsync("hi", target: ShareTarget.WhatsApp, targetApp: "com.whatsapp");

        Assert.Equal("com.whatsapp", platform.LastText?.TargetApp);
        Assert.NotNull(platform.LastText);
    }

    [Fact]
    public async Task ShareCompleted_is_raised()
    {
        var (share, _, _, _) = Harness.Create();
        ShareCompletedEventArgs? args = null;
        share.ShareCompleted += (_, value) => args = value;

        await share.ShareTextAsync("ping");

        Assert.NotNull(args);
        Assert.True(args.Result.Completed);
        Assert.Equal(ShareKind.Text, args.Result.Kind);
    }

    [Fact]
    public void CanShare_is_false_on_unsupported_platform()
    {
        var options = new SharePlusOptions();
        var share = SharePlus.Create(options, new UnsupportedSharePlatform(), new ShareFilePreparer(options, Path.GetTempPath()));

        Assert.False(share.IsSupported);
        Assert.False(share.CanShare());
    }

    [Fact]
    public async Task Unsupported_platform_throws_feature_not_supported()
    {
        var options = new SharePlusOptions();
        var share = SharePlus.Create(options, new UnsupportedSharePlatform(), new ShareFilePreparer(options, Path.GetTempPath()));

        var error = await Assert.ThrowsAsync<SharePlusException>(() => share.ShareTextAsync("nope"));
        Assert.Equal(SharePlusError.NotSupported, error.Error);
    }

    [Fact]
    public void CleanupShareCache_removes_copies()
    {
        var (share, _, preparer, _) = Harness.Create();
        var copy = Path.Combine(preparer.ShareRoot, "stale.txt");
        Directory.CreateDirectory(preparer.ShareRoot);
        File.WriteAllText(copy, "old");

        share.CleanupShareCache();

        Assert.False(File.Exists(copy));
    }
}
