using System.IO;
using System.Threading.Tasks;

namespace MCEPatcher.UI.Services;

public sealed class DesktopPlatformService : IPlatformService
{
    public int? GetDefaultAndroidVersion() => null;

    public bool CanInstallApk => false;

    public Task<bool> InstallApkAsync(string apkPath)
    {
        return Task.FromResult(false);
    }

    public string GetDefaultOutputPath(string defaultFileName)
    {
        return Path.GetFullPath(defaultFileName);
    }
}
