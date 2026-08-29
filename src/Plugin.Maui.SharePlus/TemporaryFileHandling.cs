namespace Plugin.Maui.SharePlus;

/// <summary>
/// How SharePlus prepares a file so Android <c>FileProvider</c> can serve it
/// without exposing the entire app cache (see
/// <see href="https://github.com/dotnet/maui/issues/27685">dotnet/maui#27685</see>).
/// </summary>
public enum TemporaryFileHandling
{
    /// <summary>
    /// Copy into the dedicated <c>shareplus/</c> cache root and leave the copy
    /// until the next cleanup. This is the default and the safest FileProvider path.
    /// </summary>
    CopyToShareCache = 0,

    /// <summary>
    /// Copy into the share cache and delete the copy after the share UI finishes
    /// (iOS completion handler). On Android the copy is kept until
    /// <see cref="ISharePlus.CleanupShareCache"/>, the next <c>Start</c>, or dispose,
    /// because the receiving app may still be reading the URI.
    /// </summary>
    CopyAndDeleteAfterShare = 1,

    /// <summary>
    /// Share the original path when it already lives under the share cache root;
    /// otherwise copy.
    /// </summary>
    PreferOriginal = 2,

    /// <summary>
    /// Always share the original path. Throws
    /// <see cref="SharePlusError.FileOutsideShareRoot"/> when Android FileProvider
    /// cannot serve that location.
    /// </summary>
    UseOriginal = 3
}
