using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace MCEPatcher.UI.Android;

[Activity(
    Name = "com.github.earth_restored.mcepcatcher.MainActivity",
    Label = "Minecraft Earth Patcher",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode,
    LaunchMode = LaunchMode.SingleTop,
    Exported = true)]
public sealed class MainActivity : AvaloniaMainActivity
{
}
