namespace Plugin.Maui.SharePlus.Tests;

sealed class FakeSharePlatform : ISharePlatform
{
    public bool IsSupported { get; set; } = true;

    public HashSet<ShareTarget> AvailableTargets { get; } =
    [
        ShareTarget.Any,
        ShareTarget.WhatsApp,
        ShareTarget.Email,
        ShareTarget.Messages,
        ShareTarget.Files,
        ShareTarget.NearbyShare,
        ShareTarget.AirDrop
    ];

    public ShareTextRequest? LastText { get; private set; }

    public ShareFilesRequest? LastFiles { get; private set; }

    public IReadOnlyList<PreparedShareFile>? LastPrepared { get; private set; }

    public ShareTarget LastResolved { get; private set; }

    public ShareResult NextResult { get; set; } = ShareResult.Success(ShareKind.Text, ShareTarget.Any, ShareTarget.Any);

    public bool CanShare(ShareTarget target) => AvailableTargets.Contains(target);

    public Task<ShareResult> ShareTextAsync(ShareTextRequest request, ShareTarget resolvedTarget, CancellationToken cancellationToken)
    {
        LastText = request;
        LastFiles = null;
        LastPrepared = null;
        LastResolved = resolvedTarget;
        return Task.FromResult(NextResult with
        {
            Kind = ShareKind.Text,
            RequestedTarget = request.Target,
            ResolvedTarget = resolvedTarget
        });
    }

    public Task<ShareResult> ShareFilesAsync(ShareFilesRequest request, IReadOnlyList<PreparedShareFile> files, ShareTarget resolvedTarget, CancellationToken cancellationToken)
    {
        LastText = null;
        LastFiles = request;
        LastPrepared = files;
        LastResolved = resolvedTarget;
        return Task.FromResult(NextResult with
        {
            Kind = files.Count == 1 ? ShareKind.File : ShareKind.Files,
            RequestedTarget = request.Target,
            ResolvedTarget = resolvedTarget
        });
    }
}

static class Harness
{
    public static (SharePlusImplementation Share, FakeSharePlatform Platform, ShareFilePreparer Preparer, string Root) Create(
        Action<SharePlusOptions>? configure = null)
    {
        var root = Path.Combine(Path.GetTempPath(), "shareplus-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var options = new SharePlusOptions
        {
            DefaultTitle = "SharePlus",
            DeleteShareCacheOnStart = false,
            RaiseShareCompletedEvent = true
        };
        configure?.Invoke(options);
        var platform = new FakeSharePlatform();
        var preparer = new ShareFilePreparer(options, root);
        var share = SharePlus.Create(options, platform, preparer);
        return (share, platform, preparer, root);
    }

    public static string WriteTempFile(string? directory = null, string name = "note.txt", string contents = "hello")
    {
        var folder = directory ?? Path.Combine(Path.GetTempPath(), "shareplus-src", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, name);
        File.WriteAllText(path, contents);
        return path;
    }
}
