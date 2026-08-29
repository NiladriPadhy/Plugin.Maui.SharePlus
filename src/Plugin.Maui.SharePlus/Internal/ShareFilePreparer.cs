namespace Plugin.Maui.SharePlus;

sealed class ShareFilePreparer : IShareFilePreparer
{
    readonly SharePlusOptions _options;
    readonly string? _rootOverride;

    public ShareFilePreparer(SharePlusOptions options, string? rootOverride = null)
    {
        _options = options;
        _rootOverride = rootOverride;
    }

    public string ShareRoot =>
        !string.IsNullOrWhiteSpace(_rootOverride) ? _rootOverride : ResolvePlatformCacheRoot();

    string ResolvePlatformCacheRoot()
    {
        try
        {
            return Path.Combine(FileSystem.CacheDirectory, _options.SharingRootDirectoryName);
        }
        catch (Exception)
        {
            return Path.Combine(Path.GetTempPath(), _options.SharingRootDirectoryName);
        }
    }

    public IReadOnlyList<PreparedShareFile> Prepare(IReadOnlyList<ShareFileItem> files, TemporaryFileHandling handling)
    {
        ArgumentNullException.ThrowIfNull(files);
        if (files.Count == 0)
            throw new SharePlusException(SharePlusError.InvalidRequest, "At least one file is required.");

        var prepared = new List<PreparedShareFile>(files.Count);
        foreach (var file in files)
            prepared.Add(PrepareOne(file, handling));
        return prepared;
    }

    public void Cleanup(IReadOnlyList<PreparedShareFile> files)
    {
        foreach (var file in files)
        {
            if (!file.IsTemporary)
                continue;

            try
            {
                if (File.Exists(file.SharePath))
                    File.Delete(file.SharePath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    public void CleanupShareCache()
    {
        var root = ShareRoot;
        if (!Directory.Exists(root))
            return;

        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch (IOException)
        {
            TryDeleteChildren(root);
        }
        catch (UnauthorizedAccessException)
        {
            TryDeleteChildren(root);
        }
    }

    PreparedShareFile PrepareOne(ShareFileItem file, TemporaryFileHandling handling)
    {
        if (string.IsNullOrWhiteSpace(file.FilePath))
            throw new SharePlusException(SharePlusError.InvalidRequest, "File path is required.");

        var original = Path.GetFullPath(file.FilePath);
        if (!File.Exists(original))
            throw new SharePlusException(SharePlusError.FileNotFound, $"File not found: {file.FilePath}");

        var fileName = string.IsNullOrWhiteSpace(file.FileName)
            ? Path.GetFileName(original)
            : file.FileName;
        var mime = ShareMimeTypes.Infer(file.MimeType, fileName);

        var underRoot = IsUnderShareRoot(original);
        var shouldCopy = handling switch
        {
            TemporaryFileHandling.UseOriginal => false,
            TemporaryFileHandling.PreferOriginal => !underRoot,
            _ => true
        };

        if (!shouldCopy)
        {
            if (handling == TemporaryFileHandling.UseOriginal && !underRoot)
            {
                throw new SharePlusException(
                    SharePlusError.FileOutsideShareRoot,
                    $"File is outside the share cache root '{ShareRoot}': {original}");
            }

            return new PreparedShareFile
            {
                OriginalPath = original,
                SharePath = original,
                FileName = fileName,
                MimeType = mime,
                IsTemporary = false
            };
        }

        var sharePath = CopyToShareCache(original, fileName);
        return new PreparedShareFile
        {
            OriginalPath = original,
            SharePath = sharePath,
            FileName = fileName,
            MimeType = mime,
            IsTemporary = handling == TemporaryFileHandling.CopyAndDeleteAfterShare
        };
    }

    string CopyToShareCache(string original, string fileName)
    {
        try
        {
            Directory.CreateDirectory(ShareRoot);
            var safeName = SanitizeFileName(fileName);
            var destination = Path.Combine(ShareRoot, $"{Guid.NewGuid():N}-{safeName}");
            File.Copy(original, destination, overwrite: true);
            return destination;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new SharePlusException(SharePlusError.IoFailure, $"Could not copy '{original}' into the share cache.", ex);
        }
    }

    bool IsUnderShareRoot(string fullPath)
    {
        var root = Path.GetFullPath(ShareRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(fullPath);
        return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        foreach (var invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');
        return string.IsNullOrWhiteSpace(name) ? "share.bin" : name;
    }

    static void TryDeleteChildren(string root)
    {
        foreach (var path in Directory.EnumerateFiles(root))
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
