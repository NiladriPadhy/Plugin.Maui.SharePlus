using Microsoft.Maui.Hosting;

namespace Plugin.Maui.SharePlus;

sealed class SharePlusInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var share = services.GetService<ISharePlus>() ?? SharePlus.Current;
        SharePlus.SetDefault(share);
        share.Start();
    }
}
