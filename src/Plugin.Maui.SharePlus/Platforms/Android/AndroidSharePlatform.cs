#if ANDROID
using Android.Content;
using Android.Content.PM;
using AndroidXFileProvider = AndroidX.Core.Content.FileProvider;
using AndroidUri = Android.Net.Uri;
using ClipData = Android.Content.ClipData;
using JavaFile = Java.IO.File;

namespace Plugin.Maui.SharePlus;

sealed class AndroidSharePlatform : ISharePlatform
{
    internal const string WhatsAppPackage = "com.whatsapp";
    internal const string WhatsAppBusinessPackage = "com.whatsapp.w4b";
    internal const string GmailPackage = "com.google.android.gm";
    internal const string GoogleFilesPackage = "com.google.android.apps.nbu.files";
    internal const string DocumentsUiPackage = "com.android.documentsui";
    internal const string GoogleDocumentsUiPackage = "com.google.android.documentsui";
    internal const string MessagingPackage = "com.google.android.apps.messaging";
    internal const string GmsPackage = "com.google.android.gms";
    internal const string NearbyShareActivity = "com.google.android.gms.nearby.sharing.ShareSheetActivity";

    public bool IsSupported => true;

    public bool CanShare(ShareTarget target) => target switch
    {
        ShareTarget.Any => true,
        ShareTarget.WhatsApp => IsPackageInstalled(WhatsAppPackage) || IsPackageInstalled(WhatsAppBusinessPackage),
        ShareTarget.Email => HasHandler(CreateEmailProbe()),
        ShareTarget.Messages => HasHandler(CreateSmsProbe()),
        ShareTarget.Files => true,
        ShareTarget.NearbyShare or ShareTarget.AirDrop => IsPackageInstalled(GmsPackage) || ResolveNearbyShare() is not null,
        _ => true
    };

    public Task<ShareResult> ShareTextAsync(ShareTextRequest request, ShareTarget resolvedTarget, CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            var intent = new Intent(Intent.ActionSend);
            intent.SetType(request.MimeType ?? ShareMimeTypes.TextPlain);
            intent.PutExtra(Intent.ExtraText, request.Text);
            ApplyCommonExtras(intent, request.Title, request.Subject, request.Preview);

            if (resolvedTarget == ShareTarget.Email && request.Recipients is { Count: > 0 })
                intent.PutExtra(Intent.ExtraEmail, request.Recipients.ToArray());

            if (resolvedTarget == ShareTarget.Messages && request.Recipients is { Count: > 0 } recipients)
            {
                intent = CreateSmsIntent(request.Text, recipients);
                ApplyCommonExtras(intent, request.Title, request.Subject, request.Preview);
            }

            return Launch(intent, ShareKind.Text, request.Target, resolvedTarget, request.Title, request.TargetApp);
        }, cancellationToken);

    public Task<ShareResult> ShareFilesAsync(ShareFilesRequest request, IReadOnlyList<PreparedShareFile> files, ShareTarget resolvedTarget, CancellationToken cancellationToken) =>
        RunAsync(() =>
        {
            var kind = files.Count == 1 ? ShareKind.File : ShareKind.Files;
            var uris = new List<AndroidUri>(files.Count);
            foreach (var file in files)
                uris.Add(ToShareableUri(file.SharePath));

            var mime = request.MimeType ?? ShareMimeTypes.Combine(files, null);
            var intent = files.Count == 1
                ? new Intent(Intent.ActionSend)
                : new Intent(Intent.ActionSendMultiple);
            intent.SetType(mime);

            if (files.Count == 1)
                intent.PutExtra(Intent.ExtraStream, uris[0]);
            else
                intent.PutParcelableArrayListExtra(Intent.ExtraStream, [.. uris]);

            if (!string.IsNullOrWhiteSpace(request.Text))
                intent.PutExtra(Intent.ExtraText, request.Text);

            ApplyCommonExtras(intent, request.Title, request.Subject, request.Preview);
            AttachClipData(intent, uris, request.Title ?? request.Preview?.Title);

            if (resolvedTarget == ShareTarget.Email && request.Recipients is { Count: > 0 })
                intent.PutExtra(Intent.ExtraEmail, request.Recipients.ToArray());

            return Launch(intent, kind, request.Target, resolvedTarget, request.Title, request.TargetApp, uris);
        }, cancellationToken);

    ShareResult Launch(Intent intent, ShareKind kind, ShareTarget requested, ShareTarget resolved, string? title, string? targetApp, IReadOnlyList<AndroidUri>? uris = null)
    {
        intent.AddFlags(ActivityFlags.GrantReadUriPermission);

        if (!TryApplyTarget(intent, resolved, targetApp, kind, uris, out var activityType, out var unavailable))
            return unavailable;

        var activity = Platform.CurrentActivity;
        Intent launch = intent;
        if (resolved == ShareTarget.Any && string.IsNullOrWhiteSpace(targetApp))
        {
            launch = Intent.CreateChooser(intent, title ?? "Share")
                ?? intent;
            launch.AddFlags(ActivityFlags.GrantReadUriPermission);
        }

        if (activity is null)
            launch.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
        else
            launch.AddFlags(ActivityFlags.ClearTop);

        try
        {
            GrantUriPermissions(launch, uris, activityType);
            (activity ?? AppContext()).StartActivity(launch);
            return ShareResult.Success(kind, requested, resolved, activityType);
        }
        catch (ActivityNotFoundException)
        {
            return ShareResult.Unavailable(kind, requested, resolved, $"{ShareTargetMapping.DisplayName(resolved)} is not available on this device.");
        }
        catch (Java.Lang.Exception ex)
        {
            return new ShareResult
            {
                Status = ShareStatus.Failed,
                Kind = kind,
                RequestedTarget = requested,
                ResolvedTarget = resolved,
                Message = ex.Message
            };
        }
    }

    bool TryApplyTarget(Intent intent, ShareTarget resolved, string? targetApp, ShareKind kind, IReadOnlyList<AndroidUri>? uris, out string? activityType, [NotNullWhen(false)] out ShareResult? unavailable)
    {
        activityType = null;
        unavailable = null;

        if (!string.IsNullOrWhiteSpace(targetApp))
        {
            if (!IsPackageInstalled(targetApp) && !HasHandler(CloneWithPackage(intent, targetApp)))
            {
                unavailable = ShareResult.Unavailable(kind, resolved, resolved, $"Package '{targetApp}' is not available.");
                return false;
            }

            intent.SetPackage(targetApp);
            activityType = targetApp;
            return true;
        }

        switch (resolved)
        {
            case ShareTarget.Any:
                return true;

            case ShareTarget.WhatsApp:
                var whatsApp = IsPackageInstalled(WhatsAppPackage) ? WhatsAppPackage
                    : IsPackageInstalled(WhatsAppBusinessPackage) ? WhatsAppBusinessPackage
                    : null;
                if (whatsApp is null)
                {
                    unavailable = ShareResult.Unavailable(kind, ShareTarget.WhatsApp, ShareTarget.WhatsApp, "WhatsApp is not installed.");
                    return false;
                }

                intent.SetPackage(whatsApp);
                activityType = whatsApp;
                return true;

            case ShareTarget.Email:
                if (IsPackageInstalled(GmailPackage))
                {
                    intent.SetPackage(GmailPackage);
                    activityType = GmailPackage;
                }

                if (intent.Type is null || intent.Type.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
                    intent.SetType("message/rfc822");
                return true;

            case ShareTarget.Messages:
                if (kind == ShareKind.Text && intent.Action == Intent.ActionSendto)
                {
                    activityType = "sms";
                    return true;
                }

                if (IsPackageInstalled(MessagingPackage))
                {
                    intent.SetPackage(MessagingPackage);
                    activityType = MessagingPackage;
                    return true;
                }

                activityType = "sms";
                return true;

            case ShareTarget.Files:
                var filesPackage = FirstInstalledPackage(GoogleFilesPackage, GoogleDocumentsUiPackage, DocumentsUiPackage);
                if (filesPackage is not null)
                {
                    intent.SetPackage(filesPackage);
                    activityType = filesPackage;
                }

                return true;

            case ShareTarget.NearbyShare:
            case ShareTarget.AirDrop:
                var nearby = ResolveNearbyShare();
                if (nearby is null)
                {
                    unavailable = ShareResult.Unavailable(kind, resolved, ShareTarget.NearbyShare, "Nearby Share is not available.");
                    return false;
                }

                intent.SetComponent(nearby);
                activityType = nearby.FlattenToString();
                return true;

            default:
                return true;
        }
    }

    static void ApplyCommonExtras(Intent intent, string? title, string? subject, SharePreview? preview)
    {
        var previewTitle = FirstNonEmpty(preview?.Title, title);
        if (!string.IsNullOrWhiteSpace(previewTitle))
            intent.PutExtra(Intent.ExtraTitle, previewTitle);
        if (!string.IsNullOrWhiteSpace(subject))
            intent.PutExtra(Intent.ExtraSubject, subject);
    }

    static void AttachClipData(Intent intent, IReadOnlyList<AndroidUri> uris, string? label)
    {
        if (uris.Count == 0)
            return;

        var clip = ClipData.NewRawUri(label ?? "share", uris[0])
            ?? new ClipData(label ?? "share", [intent.Type ?? ShareMimeTypes.Any], new ClipData.Item(uris[0]));
        for (var i = 1; i < uris.Count; i++)
            clip.AddItem(new ClipData.Item(uris[i]));

        intent.ClipData = clip;
        intent.AddFlags(ActivityFlags.GrantReadUriPermission);
    }

    static AndroidUri ToShareableUri(string path)
    {
        var context = AppContext();
        var file = new JavaFile(path);
        try
        {
            return AndroidXFileProvider.GetUriForFile(context, SharePlusFileProvider.Authority(context), file)
                ?? throw new SharePlusException(SharePlusError.IoFailure, "FileProvider did not return a URI.");
        }
        catch (Java.Lang.IllegalArgumentException ex)
        {
            throw new SharePlusException(
                SharePlusError.FileOutsideShareRoot,
                $"File is outside the SharePlus FileProvider root: {path}",
                ex);
        }
    }

    static void GrantUriPermissions(Intent launch, IReadOnlyList<AndroidUri>? uris, string? package)
    {
        if (uris is null || string.IsNullOrWhiteSpace(package))
            return;

        var context = AppContext();
        foreach (var uri in uris)
            context.GrantUriPermission(package, uri, ActivityFlags.GrantReadUriPermission);
    }

    static Intent CreateEmailProbe()
    {
        var intent = new Intent(Intent.ActionSendto);
        intent.SetData(AndroidUri.Parse("mailto:"));
        return intent;
    }

    static Intent CreateSmsProbe()
    {
        var intent = new Intent(Intent.ActionSendto);
        intent.SetData(AndroidUri.Parse("smsto:"));
        return intent;
    }

    static Intent CreateSmsIntent(string text, IReadOnlyList<string> recipients)
    {
        var data = AndroidUri.Parse("smsto:" + string.Join(",", recipients));
        var intent = new Intent(Intent.ActionSendto, data);
        intent.PutExtra("sms_body", text);
        intent.PutExtra(Intent.ExtraText, text);
        return intent;
    }

    static Intent CloneWithPackage(Intent source, string package)
    {
        var clone = new Intent(source);
        clone.SetPackage(package);
        return clone;
    }

    ComponentName? ResolveNearbyShare()
    {
        if (IsActivityAvailable(GmsPackage, NearbyShareActivity))
            return new ComponentName(GmsPackage, NearbyShareActivity);

        var probe = new Intent(Intent.ActionSend);
        probe.SetType(ShareMimeTypes.TextPlain);
        foreach (var resolve in Query(probe))
        {
            var name = resolve.ActivityInfo?.Name;
            var package = resolve.ActivityInfo?.PackageName;
            if (name is not null && package is not null
                && name.Contains("nearby", StringComparison.OrdinalIgnoreCase)
                && name.Contains("shar", StringComparison.OrdinalIgnoreCase))
            {
                return new ComponentName(package, name);
            }
        }

        return null;
    }

    bool IsActivityAvailable(string package, string activity)
    {
        try
        {
            var intent = new Intent();
            intent.SetClassName(package, activity);
            return HasHandler(intent);
        }
        catch (Exception)
        {
            return false;
        }
    }

    bool IsPackageInstalled(string package)
    {
        try
        {
            var pm = AppContext().PackageManager;
            if (pm is null)
                return false;

            if (OperatingSystem.IsAndroidVersionAtLeast(33))
                pm.GetPackageInfo(package, PackageManager.PackageInfoFlags.Of(0L));
            else
#pragma warning disable CS0618
                pm.GetPackageInfo(package, 0);
#pragma warning restore CS0618
            return true;
        }
        catch (PackageManager.NameNotFoundException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    bool HasHandler(Intent intent)
    {
        var list = Query(intent);
        return list.Count > 0;
    }

    IList<ResolveInfo> Query(Intent intent)
    {
        var pm = AppContext().PackageManager;
        if (pm is null)
            return [];

        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            return pm.QueryIntentActivities(intent, PackageManager.ResolveInfoFlags.Of((long)PackageInfoFlags.MatchDefaultOnly));

#pragma warning disable CS0618
        return pm.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
#pragma warning restore CS0618
    }

    string? FirstInstalledPackage(params string[] packages)
    {
        foreach (var package in packages)
        {
            if (IsPackageInstalled(package))
                return package;
        }

        return null;
    }

    static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    static Context AppContext() =>
        Android.App.Application.Context
        ?? throw new InvalidOperationException("Android application context is not available.");

    static Task<ShareResult> RunAsync(Func<ShareResult> action, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (MainThread.IsMainThread)
            return Task.FromResult(action());
        return MainThread.InvokeOnMainThreadAsync(action);
    }
}
#endif
