using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace MCEPatcher.UI.Android;

[Activity(
    Label = "Minecraft Earth Patcher",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public sealed class MainActivity : AvaloniaMainActivity
{
}
