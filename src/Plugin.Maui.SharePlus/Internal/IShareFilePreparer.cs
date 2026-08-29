namespace Plugin.Maui.SharePlus;

interface IShareFilePreparer
{
    string ShareRoot { get; }

    IReadOnlyList<PreparedShareFile> Prepare(IReadOnlyList<ShareFileItem> files, TemporaryFileHandling handling);

    void Cleanup(IReadOnlyList<PreparedShareFile> files);

    void CleanupShareCache();
}
