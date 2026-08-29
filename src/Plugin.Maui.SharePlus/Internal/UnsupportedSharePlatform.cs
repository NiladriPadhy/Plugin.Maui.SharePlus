namespace Plugin.Maui.SharePlus;

sealed class UnsupportedSharePlatform : ISharePlatform
{
    public bool IsSupported => false;

    public bool CanShare(ShareTarget target) => false;

    public Task<ShareResult> ShareTextAsync(ShareTextRequest request, ShareTarget resolvedTarget, CancellationToken cancellationToken) =>
        throw new SharePlusException(SharePlusError.NotSupported, "SharePlus requires Android or iOS.");

    public Task<ShareResult> ShareFilesAsync(ShareFilesRequest request, IReadOnlyList<PreparedShareFile> files, ShareTarget resolvedTarget, CancellationToken cancellationToken) =>
        throw new SharePlusException(SharePlusError.NotSupported, "SharePlus requires Android or iOS.");
}
