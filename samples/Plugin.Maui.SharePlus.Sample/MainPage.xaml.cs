using Plugin.Maui.SharePlus;

namespace Plugin.Maui.SharePlus.Sample;

public partial class MainPage : ContentPage
{
    readonly ISharePlus _share;
    readonly List<string> _log = [];

    public MainPage(ISharePlus share)
    {
        InitializeComponent();
        _share = share;
        TargetPicker.ItemsSource = Enum.GetValues<ShareTarget>().Select(value => value.ToString()).ToList();
        TargetPicker.SelectedIndex = 0;
        HandlingPicker.ItemsSource = Enum.GetValues<TemporaryFileHandling>().Select(value => value.ToString()).ToList();
        HandlingPicker.SelectedIndex = 0;
        _share.ShareCompleted += (_, args) => MainThread.BeginInvokeOnMainThread(() =>
        {
            ResultLabel.Text = $"{args.Result.Status} · {args.Result.ResolvedTarget} · {args.Result.ActivityType}";
            Log($"{args.Result.Status} {args.Result.Kind} {args.Result.Message}");
        });
        RefreshAvailability();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshAvailability();
    }

    async void OnShareTextClicked(object? sender, EventArgs e)
    {
        try
        {
            await _share.ShareTextAsync(
                TextEntry.Text ?? string.Empty,
                title: TitleEntry.Text,
                subject: SubjectEntry.Text,
                mimeType: MimeEntry.Text,
                preview: new SharePreview { Title = TitleEntry.Text },
                target: SelectedTarget());
        }
        catch (Exception ex)
        {
            ResultLabel.Text = ex.Message;
        }
    }

    async void OnShareFileClicked(object? sender, EventArgs e)
    {
        try
        {
            var path = await WriteDemoFileAsync("shareplus-invoice.txt", "SharePlus single-file demo");
            await _share.ShareFileAsync(
                path,
                title: TitleEntry.Text,
                subject: SubjectEntry.Text,
                mimeType: "text/plain",
                preview: new SharePreview { Title = TitleEntry.Text },
                target: SelectedTarget(),
                temporaryFileHandling: SelectedHandling(),
                text: TextEntry.Text);
        }
        catch (Exception ex)
        {
            ResultLabel.Text = ex.Message;
        }
    }

    async void OnShareFilesClicked(object? sender, EventArgs e)
    {
        try
        {
            var first = await WriteDemoFileAsync("shareplus-a.txt", "First attachment");
            var second = await WriteDemoFileAsync("shareplus-b.txt", "Second attachment");
            await _share.ShareFilesAsync(
                [first, second],
                title: TitleEntry.Text,
                subject: SubjectEntry.Text,
                preview: new SharePreview { Title = TitleEntry.Text },
                target: SelectedTarget(),
                temporaryFileHandling: SelectedHandling(),
                text: TextEntry.Text);
        }
        catch (Exception ex)
        {
            ResultLabel.Text = ex.Message;
        }
    }

    void OnCleanupClicked(object? sender, EventArgs e)
    {
        _share.CleanupShareCache();
        ResultLabel.Text = "Share cache cleaned";
        Log("cleanup");
    }

    void RefreshAvailability()
    {
        AvailabilityLabel.Text =
            $"Supported={_share.IsSupported}  Any={_share.CanShare()}  WhatsApp={_share.CanShare(ShareTarget.WhatsApp)}{Environment.NewLine}" +
            $"Email={_share.CanShare(ShareTarget.Email)}  Messages={_share.CanShare(ShareTarget.Messages)}  Files={_share.CanShare(ShareTarget.Files)}{Environment.NewLine}" +
            $"Nearby={_share.CanShare(ShareTarget.NearbyShare)}  AirDrop={_share.CanShare(ShareTarget.AirDrop)}";
    }

    ShareTarget SelectedTarget() =>
        Enum.TryParse<ShareTarget>(TargetPicker.SelectedItem?.ToString(), out var target)
            ? target
            : ShareTarget.Any;

    TemporaryFileHandling SelectedHandling() =>
        Enum.TryParse<TemporaryFileHandling>(HandlingPicker.SelectedItem?.ToString(), out var handling)
            ? handling
            : TemporaryFileHandling.CopyToShareCache;

    void Log(string line)
    {
        _log.Insert(0, $"{DateTime.Now:HH:mm:ss} {line}");
        if (_log.Count > 12)
            _log.RemoveAt(_log.Count - 1);
        LogLabel.Text = string.Join(Environment.NewLine, _log);
    }

    static async Task<string> WriteDemoFileAsync(string name, string contents)
    {
        var path = Path.Combine(FileSystem.CacheDirectory, name);
        await File.WriteAllTextAsync(path, contents);
        return path;
    }
}
