namespace Plugin.Maui.SharePlus;

/// <summary>
/// Outcome of a share request.
/// </summary>
public enum ShareStatus
{
    /// <summary>
    /// The share sheet was presented or the target app was opened.
    /// Android cannot reliably distinguish completion from cancel.
    /// </summary>
    Completed = 0,

    /// <summary>
    /// The user dismissed the share UI without sharing. iOS reports this
    /// from the activity completion handler.
    /// </summary>
    Cancelled = 1,

    /// <summary>
    /// The requested <see cref="ShareTarget"/> or <c>TargetApp</c> is not installed
    /// or cannot handle the payload.
    /// </summary>
    TargetUnavailable = 2,

    /// <summary>
    /// The share could not be started.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// The current platform does not support sharing (the <c>net10.0</c> reference assembly).
    /// </summary>
    NotSupported = 4
}
