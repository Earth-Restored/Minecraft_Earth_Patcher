using System.IO;
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
}
