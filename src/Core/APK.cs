using System.Diagnostics;

namespace MCEPatcher.Core;

public static class APK
{
    public const string FileName = "apktool.jar";
    public const string FileNameBat = "apktool.bat";

    public static bool Decode(FileInfo apk, DirectoryInfo output)
    {
        Process process;
        if (Environment.OSVersion.Platform == PlatformID.Win32NT && File.Exists(FileNameBat))
            process = U.Run(Path.GetFullPath(FileNameBat), Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), new string[]
            {
                "d",
                "-f",
                "-o", $"\"{output.FullName}\"",
                $"\"{apk.FullName}\""
            });
        else
            process = U.Run("java", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), new string[]
            {
                "-jar", $"\"{Path.GetFullPath(FileName)}\"",
                "d",
                "-f",
                "-o", $"\"{output.FullName}\"",
                $"\"{apk.FullName}\""
            });

        process.StandardInput.Write(" "); // Press any key to continue . . .

        process.WaitForExit();
        int exitCode = process.ExitCode;
        process.Close();

        return exitCode == 0;
    }

    public static bool Encode(DirectoryInfo input, FileInfo outApk)
    {
        outApk.Delete();

        Process process;
        if (Environment.OSVersion.Platform == PlatformID.Win32NT && File.Exists(FileNameBat))
            process = U.Run(Path.GetFullPath(FileNameBat), Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), new string[]
            {
                "b",
                "-f",
                "-o", $"\"{outApk.FullName}\"",
                $"\"{input.FullName}\""
            });
        else
            process = U.Run("java", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), new string[]
            {
                "-jar", $"\"{Path.GetFullPath(FileName)}\"",
                "b",
                "-f",
                "-o", $"\"{outApk.FullName}\"",
                $"\"{input.FullName}\""
            });

        process.StandardInput.Write(" "); // Press any key to continue . . .

        process.WaitForExit();
        int exitCode = process.ExitCode;
        process.Close();

        if (exitCode != 0) return false;

        outApk.Refresh();
        return outApk.Exists;
    }
}
