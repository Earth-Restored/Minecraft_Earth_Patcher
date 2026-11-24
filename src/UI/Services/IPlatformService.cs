using System.Threading.Tasks;

namespace MCEPatcher.UI.Services;

public interface IPlatformService
{
    int? GetDefaultAndroidVersion();

    bool CanInstallApk { get; }

    Task<bool> InstallApkAsync(string apkPath);

    string GetDefaultOutputPath(string defaultFileName);

    Task<string?> PublishOutputAsync(string outputPath);

    Task OpenOutputFolderAsync(string outputPath);
}
