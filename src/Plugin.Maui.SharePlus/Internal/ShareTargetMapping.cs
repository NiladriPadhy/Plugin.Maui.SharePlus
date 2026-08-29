namespace Plugin.Maui.SharePlus;

static class ShareTargetMapping
{
    public static ShareTarget Resolve(ShareTarget requested)
    {
#if ANDROID
        return requested == ShareTarget.AirDrop ? ShareTarget.NearbyShare : requested;
#elif IOS
        return requested == ShareTarget.NearbyShare ? ShareTarget.AirDrop : requested;
#else
        return requested;
#endif
    }

    public static string DisplayName(ShareTarget target) => target switch
    {
        ShareTarget.WhatsApp => "WhatsApp",
        ShareTarget.Email => "Email",
        ShareTarget.Messages => "Messages",
        ShareTarget.Files => "Files",
        ShareTarget.NearbyShare => "Nearby Share",
        ShareTarget.AirDrop => "AirDrop",
        _ => "Share"
    };
}
