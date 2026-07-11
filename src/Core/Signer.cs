using System.Diagnostics;

namespace MCEPatcher.Core;

public static class Signer
{
    public const string FileName = "uber-apk-signer.jar";

    public static bool Sign(FileInfo apkFile, DirectoryInfo outDir)
    {
        Process process = U.Run("java", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), new string[]
        {
            "-jar", $"\"{Path.GetFullPath(FileName)}\"",
            "-a", $"\"{apkFile.FullName}\"",
            "-o", $"\"{outDir.FullName}\""
        });

        process.WaitForExit();
        int exitCode = process.ExitCode;
        process.Close();
        process.Dispose();

        if (exitCode is not 0)
        {
            return false;
        }

        var outApk = outDir.EnumerateFiles().FirstOrDefault(info => info.Extension is ".apk");
        if (outApk is null)
        {
            return false;
        }

        apkFile.Delete();

        outApk.MoveTo(apkFile.FullName);

        outDir.Delete(true);

        return true;
    }
}
