#if IOS
using Foundation;
using LinkPresentation;
using MessageUI;
using UIKit;

namespace Plugin.Maui.SharePlus;

sealed class IosSharePlatform : ISharePlatform
{
    static readonly NSUrl WhatsAppScheme = new("whatsapp://");

    public bool IsSupported => true;

    public bool CanShare(ShareTarget target) => target switch
    {
        ShareTarget.Any => true,
        ShareTarget.WhatsApp => CanOpen(WhatsAppScheme),
        ShareTarget.Email => MFMailComposeViewController.CanSendMail,
        ShareTarget.Messages => MFMessageComposeViewController.CanSendText,
        ShareTarget.Files => true,
        ShareTarget.AirDrop or ShareTarget.NearbyShare => true,
        _ => true
    };

    public Task<ShareResult> ShareTextAsync(ShareTextRequest request, ShareTarget resolvedTarget, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return MainThread.InvokeOnMainThreadAsync(() => ShareText(request, resolvedTarget));
    }

    public Task<ShareResult> ShareFilesAsync(ShareFilesRequest request, IReadOnlyList<PreparedShareFile> files, ShareTarget resolvedTarget, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return MainThread.InvokeOnMainThreadAsync(() => ShareFiles(request, files, resolvedTarget));
    }

    Task<ShareResult> ShareText(ShareTextRequest request, ShareTarget resolvedTarget)
    {
        if (!string.IsNullOrWhiteSpace(request.TargetApp))
            return OpenCustomApp(request.TargetApp, request.Text, ShareKind.Text, request.Target, resolvedTarget);

        return resolvedTarget switch
        {
            ShareTarget.WhatsApp => OpenWhatsAppText(request),
            ShareTarget.Email => PresentMail(request.Subject, request.Text, request.Recipients, [], request.Target, resolvedTarget, ShareKind.Text),
            ShareTarget.Messages => PresentMessages(request.Text, request.Recipients, [], request.Target, resolvedTarget, ShareKind.Text),
            ShareTarget.Files => PresentDocumentPicker(WriteTextFile(request), request.Target, resolvedTarget, ShareKind.Text),
            ShareTarget.AirDrop => PresentActivity(CreateTextItems(request), request.Target, resolvedTarget, ShareKind.Text, airDropOnly: true),
            _ => PresentActivity(CreateTextItems(request), request.Target, resolvedTarget, ShareKind.Text, airDropOnly: false)
        };
    }

    Task<ShareResult> ShareFiles(ShareFilesRequest request, IReadOnlyList<PreparedShareFile> files, ShareTarget resolvedTarget)
    {
        var kind = files.Count == 1 ? ShareKind.File : ShareKind.Files;

        if (!string.IsNullOrWhiteSpace(request.TargetApp))
            return PresentActivity(CreateFileItems(request, files), request.Target, resolvedTarget, kind, airDropOnly: false);

        return resolvedTarget switch
        {
            ShareTarget.WhatsApp => PresentActivity(CreateFileItems(request, files), request.Target, resolvedTarget, kind, airDropOnly: false),
            ShareTarget.Email => PresentMail(request.Subject, request.Text, request.Recipients, files, request.Target, resolvedTarget, kind),
            ShareTarget.Messages => PresentMessages(request.Text, request.Recipients, files, request.Target, resolvedTarget, kind),
            ShareTarget.Files => PresentDocumentPicker(files, request.Target, resolvedTarget, kind),
            ShareTarget.AirDrop => PresentActivity(CreateFileItems(request, files), request.Target, resolvedTarget, kind, airDropOnly: true),
            _ => PresentActivity(CreateFileItems(request, files), request.Target, resolvedTarget, kind, airDropOnly: false)
        };
    }

    async Task<ShareResult> OpenWhatsAppText(ShareTextRequest request)
    {
        var url = new NSUrl("whatsapp://send?text=" + Uri.EscapeDataString(request.Text));
        if (!CanOpen(WhatsAppScheme))
            return ShareResult.Unavailable(ShareKind.Text, request.Target, ShareTarget.WhatsApp, "WhatsApp is not installed.");

        var opened = await UIApplication.SharedApplication.OpenUrlAsync(url, new UIApplicationOpenUrlOptions());
        return opened
            ? ShareResult.Success(ShareKind.Text, request.Target, ShareTarget.WhatsApp, "whatsapp")
            : ShareResult.Unavailable(ShareKind.Text, request.Target, ShareTarget.WhatsApp, "WhatsApp did not open.");
    }

    async Task<ShareResult> OpenCustomApp(string targetApp, string? text, ShareKind kind, ShareTarget requested, ShareTarget resolved)
    {
        var value = targetApp.Contains("://", StringComparison.Ordinal)
            ? targetApp
            : targetApp.TrimEnd('/') + "://";
        if (!string.IsNullOrWhiteSpace(text) && value.StartsWith("whatsapp://", StringComparison.OrdinalIgnoreCase))
            value = "whatsapp://send?text=" + Uri.EscapeDataString(text);

        var url = new NSUrl(value);
        if (url is null || !CanOpen(url))
            return ShareResult.Unavailable(kind, requested, resolved, $"App '{targetApp}' is not available.");

        var opened = await UIApplication.SharedApplication.OpenUrlAsync(url, new UIApplicationOpenUrlOptions());
        return opened
            ? ShareResult.Success(kind, requested, resolved, targetApp)
            : ShareResult.Unavailable(kind, requested, resolved, $"App '{targetApp}' did not open.");
    }

    Task<ShareResult> PresentMail(string? subject, string? body, IReadOnlyList<string>? recipients, IReadOnlyList<PreparedShareFile> files, ShareTarget requested, ShareTarget resolved, ShareKind kind)
    {
        if (!MFMailComposeViewController.CanSendMail)
            return Task.FromResult(ShareResult.Unavailable(kind, requested, resolved, "Mail is not configured on this device."));

        var tcs = new TaskCompletionSource<ShareResult>();
        var mail = new MFMailComposeViewController();
        if (!string.IsNullOrWhiteSpace(subject))
            mail.SetSubject(subject);
        if (!string.IsNullOrWhiteSpace(body))
            mail.SetMessageBody(body, isHtml: false);
        if (recipients is { Count: > 0 })
            mail.SetToRecipients(recipients.ToArray());

        foreach (var file in files)
        {
            var data = NSData.FromFile(file.SharePath);
            if (data is not null)
                mail.AddAttachmentData(data, file.MimeType, file.FileName);
        }

        mail.Finished += (_, args) =>
        {
            mail.DismissViewController(true, null);
            tcs.TrySetResult(args.Result switch
            {
                MFMailComposeResult.Sent => ShareResult.Success(kind, requested, resolved, UIActivityType.Mail),
                MFMailComposeResult.Saved => ShareResult.Success(kind, requested, resolved, UIActivityType.Mail, "Draft saved"),
                MFMailComposeResult.Cancelled => ShareResult.Cancel(kind, requested, resolved),
                _ => new ShareResult
                {
                    Status = ShareStatus.Failed,
                    Kind = kind,
                    RequestedTarget = requested,
                    ResolvedTarget = resolved,
                    Message = args.Error?.LocalizedDescription ?? "Mail failed."
                }
            });
        };

        Present(mail);
        return tcs.Task;
    }

    Task<ShareResult> PresentMessages(string? body, IReadOnlyList<string>? recipients, IReadOnlyList<PreparedShareFile> files, ShareTarget requested, ShareTarget resolved, ShareKind kind)
    {
        if (!MFMessageComposeViewController.CanSendText)
            return Task.FromResult(ShareResult.Unavailable(kind, requested, resolved, "Messages cannot send text on this device."));

        var tcs = new TaskCompletionSource<ShareResult>();
        var composer = new MFMessageComposeViewController();
        if (!string.IsNullOrWhiteSpace(body))
            composer.Body = body;
        if (recipients is { Count: > 0 })
            composer.Recipients = recipients.ToArray();

        if (files.Count > 0 && MFMessageComposeViewController.CanSendAttachments)
        {
            foreach (var file in files)
                composer.AddAttachment(file.SharePath, file.MimeType, file.FileName);
        }

        composer.Finished += (_, args) =>
        {
            composer.DismissViewController(true, null);
            tcs.TrySetResult(args.Result switch
            {
                MessageComposeResult.Sent => ShareResult.Success(kind, requested, resolved, UIActivityType.Message),
                MessageComposeResult.Cancelled => ShareResult.Cancel(kind, requested, resolved),
                _ => new ShareResult
                {
                    Status = ShareStatus.Failed,
                    Kind = kind,
                    RequestedTarget = requested,
                    ResolvedTarget = resolved,
                    Message = "Messages failed."
                }
            });
        };

        Present(composer);
        return tcs.Task;
    }

    Task<ShareResult> PresentDocumentPicker(IReadOnlyList<PreparedShareFile> files, ShareTarget requested, ShareTarget resolved, ShareKind kind)
    {
        var urls = files
            .Select(file => NSUrl.FromFilename(file.SharePath))
            .Where(url => url is not null)
            .Cast<NSUrl>()
            .ToArray();

        if (urls.Length == 0)
            return Task.FromResult(new ShareResult
            {
                Status = ShareStatus.Failed,
                Kind = kind,
                RequestedTarget = requested,
                ResolvedTarget = resolved,
                Message = "No files were available to export."
            });

        var tcs = new TaskCompletionSource<ShareResult>();
        var picker = new UIDocumentPickerViewController(urls, asCopy: true);
        picker.DidPickDocumentAtUrls += (_, _) =>
            tcs.TrySetResult(ShareResult.Success(kind, requested, resolved, "com.apple.DocumentManagerUICore.SaveToFiles"));
        picker.WasCancelled += (_, _) =>
            tcs.TrySetResult(ShareResult.Cancel(kind, requested, resolved));

        Present(picker);
        return tcs.Task;
    }

    Task<ShareResult> PresentActivity(NSObject[] items, ShareTarget requested, ShareTarget resolved, ShareKind kind, bool airDropOnly)
    {
        var tcs = new TaskCompletionSource<ShareResult>();
        var controller = new UIActivityViewController(items, applicationActivities: null);
        if (airDropOnly)
            controller.ExcludedActivityTypes = AllActivitiesExceptAirDrop();

        controller.CompletionWithItemsHandler = (activityType, completed, _, error) =>
        {
            if (error is not null)
            {
                tcs.TrySetResult(new ShareResult
                {
                    Status = ShareStatus.Failed,
                    Kind = kind,
                    RequestedTarget = requested,
                    ResolvedTarget = resolved,
                    ActivityType = activityType,
                    Message = error.LocalizedDescription
                });
                return;
            }

            tcs.TrySetResult(completed
                ? ShareResult.Success(kind, requested, resolved, activityType)
                : ShareResult.Cancel(kind, requested, resolved));
        };

        Present(controller);
        return tcs.Task;
    }

    static NSObject[] CreateTextItems(ShareTextRequest request) =>
        [new SharePlusActivityItem(new NSString(request.Text), request.Title, request.Subject, request.MimeType ?? ShareMimeTypes.TextPlain, request.Preview, itemUrl: null)];

    static NSObject[] CreateFileItems(ShareFilesRequest request, IReadOnlyList<PreparedShareFile> files)
    {
        var items = new List<NSObject>(files.Count + 1);
        if (!string.IsNullOrWhiteSpace(request.Text))
            items.Add(new SharePlusActivityItem(new NSString(request.Text), request.Title, request.Subject, ShareMimeTypes.TextPlain, request.Preview, itemUrl: null));

        foreach (var file in files)
        {
            var url = NSUrl.FromFilename(file.SharePath);
            if (url is null)
                continue;
            items.Add(new SharePlusActivityItem(url, request.Title, request.Subject, file.MimeType, request.Preview, url));
        }

        return [.. items];
    }

    static IReadOnlyList<PreparedShareFile> WriteTextFile(ShareTextRequest request)
    {
        var directory = Path.Combine(Path.GetTempPath(), "shareplus");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, request.Text);
        return
        [
            new PreparedShareFile
            {
                OriginalPath = path,
                SharePath = path,
                FileName = "share.txt",
                MimeType = request.MimeType ?? ShareMimeTypes.TextPlain,
                IsTemporary = true
            }
        ];
    }

    static void Present(UIViewController controller)
    {
        var presenter = CurrentViewController();
        if (controller.PopoverPresentationController is { } popover && presenter.View is { } source)
        {
            popover.SourceView = source;
            popover.SourceRect = new CoreGraphics.CGRect(source.Bounds.Left + source.Bounds.Width / 2, source.Bounds.Top + source.Bounds.Height / 2, 0, 0);
            popover.PermittedArrowDirections = 0;
        }

        presenter.PresentViewController(controller, animated: true, completionHandler: null);
    }

    static UIViewController CurrentViewController()
    {
        var controller = Platform.GetCurrentUIViewController();
        if (controller is not null)
            return controller;

        throw new InvalidOperationException("No current iOS view controller is available to present the share UI.");
    }

    static bool CanOpen(NSUrl url)
    {
        try
        {
            return UIApplication.SharedApplication.CanOpenUrl(url);
        }
        catch (Exception)
        {
            return false;
        }
    }

    static NSString[] AllActivitiesExceptAirDrop() =>
    [
        UIActivityType.PostToFacebook,
        UIActivityType.PostToTwitter,
        UIActivityType.PostToWeibo,
        UIActivityType.Message,
        UIActivityType.Mail,
        UIActivityType.Print,
        UIActivityType.CopyToPasteboard,
        UIActivityType.AssignToContact,
        UIActivityType.SaveToCameraRoll,
        UIActivityType.AddToReadingList,
        UIActivityType.PostToFlickr,
        UIActivityType.PostToVimeo,
        UIActivityType.PostToTencentWeibo,
        UIActivityType.OpenInIBooks,
        UIActivityType.MarkupAsPdf
    ];
}

sealed class SharePlusActivityItem : NSObject, IUIActivityItemSource
{
    readonly NSObject _item;
    readonly string? _title;
    readonly string? _subject;
    readonly string _uti;
    readonly UIImage? _thumbnail;
    readonly LPLinkMetadata _metadata;

    public SharePlusActivityItem(NSObject item, string? title, string? subject, string mimeType, SharePreview? preview, NSUrl? itemUrl)
    {
        _item = item;
        _title = FirstNonEmpty(preview?.Title, title);
        _subject = subject;
        _uti = ToUti(mimeType);
        _thumbnail = LoadThumbnail(preview);
        _metadata = new LPLinkMetadata
        {
            Title = _title ?? subject ?? "Share",
            OriginalUrl = itemUrl,
            Url = itemUrl
        };
        if (_thumbnail is not null)
            _metadata.IconProvider = new NSItemProvider(_thumbnail);
    }

    public NSObject GetPlaceholderData(UIActivityViewController activityViewController) => _item;

    public NSObject? GetItemForActivity(UIActivityViewController activityViewController, NSString? activityType) => _item;

    [Export("activityViewController:subjectForActivityType:")]
    public string GetSubjectForActivity(UIActivityViewController activityViewController, NSString? activityType) =>
        _subject ?? _title ?? string.Empty;

    [Export("activityViewController:dataTypeIdentifierForActivityType:")]
    public string GetDataTypeIdentifierForActivity(UIActivityViewController activityViewController, NSString? activityType) =>
        _uti;

    [Export("activityViewController:thumbnailImageForActivityType:suggestedSize:")]
    public UIImage GetThumbnailImageForActivity(UIActivityViewController activityViewController, NSString? activityType, CoreGraphics.CGSize suggestedSize) =>
        _thumbnail ?? new UIImage();

    [Export("activityViewControllerLinkMetadata:")]
    public LPLinkMetadata GetLinkMetadata(UIActivityViewController activityViewController) => _metadata;

    static UIImage? LoadThumbnail(SharePreview? preview)
    {
        if (preview is null)
            return null;

        if (!string.IsNullOrWhiteSpace(preview.ThumbnailFilePath) && File.Exists(preview.ThumbnailFilePath))
            return UIImage.FromFile(preview.ThumbnailFilePath);

        if (preview.ThumbnailBytes is { Length: > 0 })
            return UIImage.LoadFromData(NSData.FromArray(preview.ThumbnailBytes));

        return null;
    }

    static string ToUti(string mimeType) => mimeType.ToLowerInvariant() switch
    {
        "text/plain" => "public.plain-text",
        "text/html" => "public.html",
        "application/pdf" => "com.adobe.pdf",
        "image/png" => "public.png",
        "image/jpeg" or "image/jpg" => "public.jpeg",
        "image/gif" => "com.compuserve.gif",
        "image/heic" => "public.heic",
        "video/mp4" => "public.mpeg-4",
        _ => "public.data"
    };

    static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }
}
#endif
