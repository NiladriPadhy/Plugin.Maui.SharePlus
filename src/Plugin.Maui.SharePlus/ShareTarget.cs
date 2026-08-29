namespace Plugin.Maui.SharePlus;

/// <summary>
/// Destination for a share request. <see cref="Any"/> shows the system share sheet.
/// Specific values open or filter to that app when it is installed.
/// </summary>
public enum ShareTarget
{
    /// <summary>
    /// System share sheet (Android chooser / iOS <c>UIActivityViewController</c>).
    /// </summary>
    Any = 0,

    /// <summary>
    /// WhatsApp. Android uses <c>com.whatsapp</c> (Business as fallback).
    /// iOS uses the <c>whatsapp://</c> scheme for text.
    /// </summary>
    WhatsApp = 1,

    /// <summary>
    /// Email. Android uses <c>ACTION_SEND</c> / <c>mailto</c>.
    /// iOS uses <c>MFMailComposeViewController</c>.
    /// </summary>
    Email = 2,

    /// <summary>
    /// SMS / Messages. Android uses <c>smsto:</c>.
    /// iOS uses <c>MFMessageComposeViewController</c>.
    /// </summary>
    Messages = 3,

    /// <summary>
    /// Files app / document manager. Android targets Files or DocumentsUI.
    /// iOS presents a document picker export.
    /// </summary>
    Files = 4,

    /// <summary>
    /// Android Nearby Share. On iOS this maps to <see cref="AirDrop"/>.
    /// </summary>
    NearbyShare = 5,

    /// <summary>
    /// iOS AirDrop. On Android this maps to <see cref="NearbyShare"/>.
    /// </summary>
    AirDrop = 6
}
