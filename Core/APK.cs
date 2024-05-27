using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCEPatcher.Core
{
    public static class APK
    {
        const string apkToolName = "apktool.bat";

        public static bool Decode(FileInfo apk, DirectoryInfo output)
        {
            Process process;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT && File.Exists(apkToolName))
                process = U.Run(Path.GetFullPath(apkToolName), Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), new string[]
                {
                    "d",
                    "-f",
                    "-o", $"\"{output.FullName}\"",
                    $"\"{apk.FullName}\""
                });
            else
                process = U.Run("java", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), new string[]
                {
                    "-jar", $"\"{Path.GetFullPath("apktool.jar")}\"",
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
            if (Environment.OSVersion.Platform == PlatformID.Win32NT && File.Exists(apkToolName)) 
                process = U.Run(Path.GetFullPath(apkToolName), Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), new string[]
                {
                    "b",
                    "-f",
                    "-o", $"\"{outApk.FullName}\"",
                    $"\"{input.FullName}\""
                });
            else
                process = U.Run("java", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), new string[]
                {
                    "-jar", $"\"{Path.GetFullPath("apktool.jar")}\"",
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
}
