namespace Plugin.Maui.SharePlus;

/// <summary>
/// Outcome of a SharePlus text or file request.
/// </summary>
public sealed record ShareResult
{
    /// <summary>
    /// Gets the classified status.
    /// </summary>
    public required ShareStatus Status { get; init; }

    /// <summary>
    /// Gets the payload kind.
    /// </summary>
    public required ShareKind Kind { get; init; }

    /// <summary>
    /// Gets the target that was requested.
    /// </summary>
    public ShareTarget RequestedTarget { get; init; }

    /// <summary>
    /// Gets the target that was actually used after platform mapping
    /// (AirDrop ↔ Nearby Share).
    /// </summary>
    public ShareTarget ResolvedTarget { get; init; }

    /// <summary>
    /// iOS activity type or Android package / component that handled the share,
    /// when the platform reports one.
    /// </summary>
    public string? ActivityType { get; init; }

    /// <summary>
    /// Human-readable status message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Whether the share UI was presented or the target app was opened.
    /// </summary>
    public bool Completed => Status == ShareStatus.Completed;

    /// <summary>
    /// Creates a completed result.
    /// </summary>
    public static ShareResult Success(ShareKind kind, ShareTarget requested, ShareTarget resolved, string? activityType = null, string? message = null) =>
        new()
        {
            Status = ShareStatus.Completed,
            Kind = kind,
            RequestedTarget = requested,
            ResolvedTarget = resolved,
            ActivityType = activityType,
            Message = message ?? "Shared"
        };

    /// <summary>
    /// Creates a cancelled result.
    /// </summary>
    public static ShareResult Cancel(ShareKind kind, ShareTarget requested, ShareTarget resolved, string? message = null) =>
        new()
        {
            Status = ShareStatus.Cancelled,
            Kind = kind,
            RequestedTarget = requested,
            ResolvedTarget = resolved,
            Message = message ?? "Cancelled"
        };

    /// <summary>
    /// Creates a target-unavailable result.
    /// </summary>
    public static ShareResult Unavailable(ShareKind kind, ShareTarget requested, ShareTarget resolved, string? message = null) =>
        new()
        {
            Status = ShareStatus.TargetUnavailable,
            Kind = kind,
            RequestedTarget = requested,
            ResolvedTarget = resolved,
            Message = message ?? "The requested share target is not available."
        };
}
