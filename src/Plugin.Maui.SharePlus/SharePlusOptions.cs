namespace Plugin.Maui.SharePlus;

/// <summary>
/// Process-wide defaults for <see cref="ISharePlus"/>.
/// </summary>
public sealed class SharePlusOptions
{
    /// <summary>
    /// Chooser / activity title used when a request omits <c>Title</c>.
    /// </summary>
    public string? DefaultTitle { get; set; }

    /// <summary>
    /// File handling used when a request does not set
    /// <see cref="ShareFileRequest.TemporaryFileHandling"/>.
    /// Default is <see cref="TemporaryFileHandling.CopyToShareCache"/>.
    /// </summary>
    public TemporaryFileHandling DefaultTemporaryFileHandling { get; set; } = TemporaryFileHandling.CopyToShareCache;

    /// <summary>
    /// Subdirectory under the app cache that FileProvider is allowed to serve.
    /// Default is <c>shareplus</c>. Do not expose the entire cache.
    /// </summary>
    public string SharingRootDirectoryName { get; set; } = "shareplus";

    /// <summary>
    /// When <c>true</c>, leftover copies in the share cache are deleted on <c>Start</c>.
    /// Default is <c>true</c>.
    /// </summary>
    public bool DeleteShareCacheOnStart { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, a missing target app throws
    /// <see cref="SharePlusException"/> instead of returning
    /// <see cref="ShareStatus.TargetUnavailable"/>. Default is <c>false</c>.
    /// </summary>
    public bool ThrowWhenTargetUnavailable { get; set; }

    /// <summary>
    /// When <c>true</c> and the requested target is missing, the system share
    /// sheet is shown instead. Default is <c>false</c>.
    /// </summary>
    public bool FallbackToShareSheetWhenTargetUnavailable { get; set; }

    /// <summary>
    /// When <c>true</c>, successful or cancelled shares raise
    /// <see cref="ISharePlus.ShareCompleted"/>. Default is <c>true</c>.
    /// </summary>
    public bool RaiseShareCompletedEvent { get; set; } = true;
}
