using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MCEPatcher.UI.Services;

public sealed class DesktopPlatformService : IPlatformService
{
    public int? GetDefaultAndroidVersion()
        => null;

    public bool CanInstallApk => false;

    public async Task<bool> InstallApkAsync(string apkPath)
        => false;

    public string GetDefaultOutputPath(string defaultFileName)
        => Path.GetFullPath(defaultFileName);

    public async Task<string?> PublishOutputAsync(string outputPath)
        => Path.GetFullPath(outputPath);

    public async Task OpenOutputFolderAsync(string outputPath)
    {
        var filePath = Path.GetFullPath(outputPath);
        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", Path.GetDirectoryName(filePath)!);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", $"-R \"{filePath}\"");
            }
        }
        catch (Exception exception)
        {
            Log.Warning($"Failed to open output folder: {exception.Message}");
        }
    }
}
