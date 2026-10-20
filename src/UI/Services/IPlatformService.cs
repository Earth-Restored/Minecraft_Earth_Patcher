using System.Threading.Tasks;

namespace MCEPatcher.UI.Services;

public interface IPlatformService
{
    /// <summary>
    /// Gets the default Android OS version for the device (e.g. 14 for Android 14).
    /// Returns null if not running on Android or cannot be determined.
    /// </summary>
    int? GetDefaultAndroidVersion();

    /// <summary>
    /// Indicates whether the current platform supports direct in-app APK installation prompt.
    /// </summary>
    bool CanInstallApk { get; }

    /// <summary>
    /// Prompts the user to install the patched APK file, requesting unknown sources permissions if necessary.
    /// </summary>
    Task<bool> InstallApkAsync(string apkPath);

    /// <summary>
    /// Gets the default output path for patched APK or IPA files.
    /// </summary>
    string GetDefaultOutputPath(string defaultFileName);
}
