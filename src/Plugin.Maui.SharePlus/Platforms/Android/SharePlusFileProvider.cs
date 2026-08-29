#if ANDROID
using Android.App;
using Android.Content;
using AndroidXFileProvider = AndroidX.Core.Content.FileProvider;

namespace Plugin.Maui.SharePlus;

/// <summary>
/// FileProvider that exposes only the <c>shareplus/</c> sharing root.
/// This avoids granting the entire app cache the way the default MAUI
/// FileProvider can (see <see href="https://github.com/dotnet/maui/issues/27685">dotnet/maui#27685</see>).
/// </summary>
[ContentProvider(
    new[] { "${applicationId}.shareplus.fileprovider" },
    Name = "plugin.maui.shareplus.SharePlusFileProvider",
    Exported = false,
    GrantUriPermissions = true)]
[MetaData("android.support.FILE_PROVIDER_PATHS", Resource = "@xml/shareplus_file_paths")]
public class SharePlusFileProvider : AndroidXFileProvider
{
    internal static string Authority(Context context) => context.PackageName + ".shareplus.fileprovider";
}
#endif
