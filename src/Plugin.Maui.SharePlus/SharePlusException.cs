namespace Plugin.Maui.SharePlus;

/// <summary>
/// Thrown when a share request cannot be started.
/// </summary>
public sealed class SharePlusException : Exception
{
    /// <summary>
    /// Initializes a new exception with an error code and message.
    /// </summary>
    public SharePlusException(SharePlusError error, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Error = error;
    }

    /// <summary>
    /// Gets the classified error.
    /// </summary>
    public SharePlusError Error { get; }
}
