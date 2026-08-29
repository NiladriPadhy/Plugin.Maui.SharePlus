namespace Plugin.Maui.SharePlus;

/// <summary>
/// Payload kind for a share request.
/// </summary>
public enum ShareKind
{
    /// <summary>Plain or rich text.</summary>
    Text = 0,

    /// <summary>A single file.</summary>
    File = 1,

    /// <summary>Two or more files.</summary>
    Files = 2
}
