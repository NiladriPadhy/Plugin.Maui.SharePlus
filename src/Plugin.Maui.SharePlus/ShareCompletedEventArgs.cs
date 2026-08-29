namespace Plugin.Maui.SharePlus;

/// <summary>
/// Raised after a share attempt finishes.
/// </summary>
public sealed class ShareCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes event data.
    /// </summary>
    public ShareCompletedEventArgs(ShareResult result)
    {
        Result = result;
    }

    /// <summary>
    /// Gets the share outcome.
    /// </summary>
    public ShareResult Result { get; }
}
