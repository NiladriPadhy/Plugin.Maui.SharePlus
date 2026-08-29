namespace Plugin.Maui.SharePlus;

sealed class SharePlusImplementation : ISharePlus
{
    readonly SharePlusOptions _options;
    readonly ISharePlatform _platform;
    readonly IShareFilePreparer _preparer;
    readonly object _gate = new();
    bool _started;
    bool _disposed;

    public SharePlusImplementation(SharePlusOptions options, ISharePlatform platform, IShareFilePreparer preparer)
    {
        _options = options;
        _platform = platform;
        _preparer = preparer;
    }

    public bool IsSupported => _platform.IsSupported;

    public event EventHandler<ShareCompletedEventArgs>? ShareCompleted;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started)
            return;

        if (_options.DeleteShareCacheOnStart)
            _preparer.CleanupShareCache();

        _started = true;
    }

    public bool CanShare(ShareTarget target = ShareTarget.Any)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_platform.IsSupported)
            return false;

        return _platform.CanShare(ShareTargetMapping.Resolve(target));
    }

    public Task<ShareResult> ShareTextAsync(string text, string? title = null, string? subject = null, string? mimeType = null, SharePreview? preview = null, ShareTarget target = ShareTarget.Any, string? targetApp = null, IReadOnlyList<string>? recipients = null, CancellationToken cancellationToken = default) =>
        ShareTextAsync(new ShareTextRequest
        {
            Text = text,
            Title = title,
            Subject = subject,
            MimeType = mimeType,
            Preview = preview,
            Target = target,
            TargetApp = targetApp,
            Recipients = recipients
        }, cancellationToken);

    public async Task<ShareResult> ShareTextAsync(ShareTextRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        EnsureSupported();

        if (string.IsNullOrWhiteSpace(request.Text))
            throw new SharePlusException(SharePlusError.InvalidRequest, "Text is required.");

        var normalized = NormalizeText(request);
        if (!TryResolveTarget(ShareKind.Text, normalized.Target, normalized.TargetApp, out var resolved, out var unavailable))
            return Raise(unavailable);

        var result = await _platform.ShareTextAsync(normalized, resolved, cancellationToken).ConfigureAwait(false);
        return HandlePlatformResult(result);
    }

    public Task<ShareResult> ShareFileAsync(string filePath, string? title = null, string? subject = null, string? mimeType = null, SharePreview? preview = null, ShareTarget target = ShareTarget.Any, TemporaryFileHandling temporaryFileHandling = TemporaryFileHandling.CopyToShareCache, string? text = null, string? fileName = null, string? targetApp = null, IReadOnlyList<string>? recipients = null, CancellationToken cancellationToken = default) =>
        ShareFileAsync(new ShareFileRequest
        {
            FilePath = filePath,
            FileName = fileName,
            Title = title,
            Subject = subject,
            MimeType = mimeType,
            Text = text,
            Preview = preview,
            Target = target,
            TargetApp = targetApp,
            TemporaryFileHandling = temporaryFileHandling,
            Recipients = recipients
        }, cancellationToken);

    public Task<ShareResult> ShareFileAsync(ShareFileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ShareFilesAsync(new ShareFilesRequest
        {
            Files =
            [
                new ShareFileItem
                {
                    FilePath = request.FilePath,
                    FileName = request.FileName,
                    MimeType = request.MimeType
                }
            ],
            Title = request.Title,
            Subject = request.Subject,
            MimeType = request.MimeType,
            Text = request.Text,
            Preview = request.Preview,
            Target = request.Target,
            TargetApp = request.TargetApp,
            TemporaryFileHandling = request.TemporaryFileHandling,
            Recipients = request.Recipients
        }, cancellationToken);
    }

    public Task<ShareResult> ShareFilesAsync(IEnumerable<string> filePaths, string? title = null, string? subject = null, string? mimeType = null, SharePreview? preview = null, ShareTarget target = ShareTarget.Any, TemporaryFileHandling temporaryFileHandling = TemporaryFileHandling.CopyToShareCache, string? text = null, string? targetApp = null, IReadOnlyList<string>? recipients = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        var items = filePaths
            .Select(path => new ShareFileItem { FilePath = path })
            .ToArray();

        return ShareFilesAsync(new ShareFilesRequest
        {
            Files = items,
            Title = title,
            Subject = subject,
            MimeType = mimeType,
            Text = text,
            Preview = preview,
            Target = target,
            TargetApp = targetApp,
            TemporaryFileHandling = temporaryFileHandling,
            Recipients = recipients
        }, cancellationToken);
    }

    public async Task<ShareResult> ShareFilesAsync(ShareFilesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        EnsureSupported();

        var handling = request.TemporaryFileHandling;
        var prepared = _preparer.Prepare(request.Files, handling);
        var normalized = NormalizeFiles(request, prepared);
        var kind = prepared.Count == 1 ? ShareKind.File : ShareKind.Files;

        try
        {
            if (!TryResolveTarget(kind, normalized.Target, normalized.TargetApp, out var resolved, out var unavailable))
                return Raise(unavailable);

            var result = await _platform.ShareFilesAsync(normalized, prepared, resolved, cancellationToken).ConfigureAwait(false);
            result = result with { Kind = kind };
            var handled = HandlePlatformResult(result);

            if (handling == TemporaryFileHandling.CopyAndDeleteAfterShare
                && handled.Status is ShareStatus.Completed or ShareStatus.Cancelled)
            {
#if !ANDROID
                _preparer.Cleanup(prepared);
#endif
            }

            return handled;
        }
        catch
        {
            if (handling == TemporaryFileHandling.CopyAndDeleteAfterShare)
                _preparer.Cleanup(prepared);
            throw;
        }
    }

    public void CleanupShareCache()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _preparer.CleanupShareCache();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
        }

        if (_options.DeleteShareCacheOnStart)
            _preparer.CleanupShareCache();
    }

    void EnsureSupported()
    {
        if (_platform.IsSupported)
            return;

        throw new SharePlusException(SharePlusError.NotSupported, "SharePlus requires Android or iOS.");
    }

    bool TryResolveTarget(ShareKind kind, ShareTarget requested, string? targetApp, out ShareTarget resolved, [NotNullWhen(false)] out ShareResult? unavailable)
    {
        resolved = ShareTargetMapping.Resolve(requested);
        unavailable = null;

        if (resolved == ShareTarget.Any || !string.IsNullOrWhiteSpace(targetApp) || _platform.CanShare(resolved))
            return true;

        if (_options.FallbackToShareSheetWhenTargetUnavailable)
        {
            resolved = ShareTarget.Any;
            return true;
        }

        var result = ShareResult.Unavailable(kind, requested, resolved, $"{ShareTargetMapping.DisplayName(resolved)} is not available on this device.");
        if (_options.ThrowWhenTargetUnavailable)
            throw new SharePlusException(SharePlusError.TargetUnavailable, result.Message ?? "Target unavailable.");

        unavailable = result;
        return false;
    }

    ShareTextRequest NormalizeText(ShareTextRequest request) =>
        new()
        {
            Text = request.Text,
            Title = FirstNonEmpty(request.Title, request.Preview?.Title, _options.DefaultTitle),
            Subject = request.Subject,
            MimeType = string.IsNullOrWhiteSpace(request.MimeType) ? ShareMimeTypes.TextPlain : request.MimeType,
            Preview = request.Preview,
            Target = request.Target,
            TargetApp = request.TargetApp,
            Recipients = request.Recipients
        };

    ShareFilesRequest NormalizeFiles(ShareFilesRequest request, IReadOnlyList<PreparedShareFile> prepared) =>
        new()
        {
            Files = request.Files,
            Title = FirstNonEmpty(request.Title, request.Preview?.Title, _options.DefaultTitle),
            Subject = request.Subject,
            MimeType = ShareMimeTypes.Combine(prepared, request.MimeType),
            Text = request.Text,
            Preview = request.Preview,
            Target = request.Target,
            TargetApp = request.TargetApp,
            TemporaryFileHandling = request.TemporaryFileHandling,
            Recipients = request.Recipients
        };

    ShareResult HandlePlatformResult(ShareResult result)
    {
        if (result.Status == ShareStatus.TargetUnavailable)
        {
            if (_options.FallbackToShareSheetWhenTargetUnavailable && result.ResolvedTarget != ShareTarget.Any)
            {
                // Platform already failed to open a specific target; caller should retry with Any if desired.
            }

            if (_options.ThrowWhenTargetUnavailable)
                throw new SharePlusException(SharePlusError.TargetUnavailable, result.Message ?? "Target unavailable.");
        }

        return Raise(result);
    }

    ShareResult Raise(ShareResult result)
    {
        if (_options.RaiseShareCompletedEvent)
            ShareCompleted?.Invoke(this, new ShareCompletedEventArgs(result));
        return result;
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
}
