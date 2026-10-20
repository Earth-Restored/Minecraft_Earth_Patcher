using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using AndroidX.Core.Content;
using MCEPatcher.UI.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MCEPatcher.UI.Android;

public sealed class AndroidPlatformService : IPlatformService
{
    private readonly Context _context;

    public AndroidPlatformService(Context context)
    {
        _context = context.ApplicationContext ?? context;
    }

    public int? GetDefaultAndroidVersion()
    {
        if (!string.IsNullOrEmpty(Build.VERSION.Release))
        {
            var parts = Build.VERSION.Release.Split('.');
            if (int.TryParse(parts[0], out int releaseVer) && releaseVer >= 7)
            {
                return releaseVer;
            }
        }

        return (int)Build.VERSION.SdkInt switch
        {
            >= 35 => 15,
            34 => 14,
            33 => 13,
            >= 31 => 12,
            30 => 11,
            29 => 10,
            28 => 9,
            >= 26 => 8,
            _ => 7
        };
    }

    public bool CanInstallApk => true;

    public Task<bool> InstallApkAsync(string apkPath)
    {
        try
        {
            var javaFile = new Java.IO.File(apkPath);
            if (!javaFile.Exists())
            {
                return Task.FromResult(false);
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                if (!_context.PackageManager!.CanRequestPackageInstalls())
                {
                    var settingsIntent = new Intent(Settings.ActionManageUnknownAppSources);
                    settingsIntent.SetData(global::Android.Net.Uri.Parse($"package:{_context.PackageName}"));
                    settingsIntent.SetFlags(ActivityFlags.NewTask);
                    _context.StartActivity(settingsIntent);
                    return Task.FromResult(false);
                }
            }

            var apkUri = FileProvider.GetUriForFile(
                _context,
                $"{_context.PackageName}.fileprovider",
                javaFile);

            var installIntent = new Intent(Intent.ActionView);
            installIntent.SetDataAndType(apkUri, "application/vnd.android.package-archive");
            installIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);
            _context.StartActivity(installIntent);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error($"Failed to trigger APK install: {ex}");
            return Task.FromResult(false);
        }
    }

    public string GetDefaultOutputPath(string defaultFileName)
    {
        var extDir = _context.GetExternalFilesDir(null)?.AbsolutePath
            ?? _context.FilesDir?.AbsolutePath
            ?? global::Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath
            ?? "/sdcard/Download";

        return Path.Combine(extDir, defaultFileName);
    }
}
