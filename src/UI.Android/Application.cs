using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using MCEPatcher.Core;
using MCEPatcher.UI.Services;

namespace MCEPatcher.UI.Android;

[Application]
public sealed class Application : AvaloniaAndroidApplication<App>
{
    public Application(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        PlatformService.Current = new AndroidPlatformService(this);
        ApkProcessor.ToolService = new AndroidApkToolService(this);
        
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
