namespace Plugin.Maui.SharePlus;

interface ISharePlatform
{
    bool IsSupported { get; }

    bool CanShare(ShareTarget target);

    Task<ShareResult> ShareTextAsync(ShareTextRequest request, ShareTarget resolvedTarget, CancellationToken cancellationToken);

    Task<ShareResult> ShareFilesAsync(ShareFilesRequest request, IReadOnlyList<PreparedShareFile> files, ShareTarget resolvedTarget, CancellationToken cancellationToken);
}
