namespace Plugin.Maui.SharePlus;

/// <summary>
/// Classifies a <see cref="SharePlusException"/>.
/// </summary>
public enum SharePlusError
{
    /// <summary>The request is missing required fields.</summary>
    InvalidRequest = 0,

    /// <summary>A file path does not exist.</summary>
    FileNotFound = 1,

    /// <summary>
    /// The file is outside the FileProvider sharing root and
    /// <see cref="TemporaryFileHandling.UseOriginal"/> was requested.
    /// </summary>
    FileOutsideShareRoot = 2,

    /// <summary>The requested target app is not installed or cannot share this payload.</summary>
    TargetUnavailable = 3,

    /// <summary>The current platform cannot share.</summary>
    NotSupported = 4,

    /// <summary>A file could not be copied into the share cache.</summary>
    IoFailure = 5
}
