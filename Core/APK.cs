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
                process = U.Run(apkToolName, new string[]
                {
                    "/c",
                    "d",
                    "-f",
                    "-o", $"\"{output.FullName}\"",
                    $"\"{apk.FullName}\""
                });
            else
                process = U.Run("java", new string[]
                {
                    "-jar", "apktool.jar",
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
            Process process;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT && File.Exists(apkToolName)) 
                process = U.Run(apkToolName, new string[]
                {
                    "/c",
                    "b",
                    "-f",
                    "-o", $"\"{outApk.FullName}\"",
                    $"\"{input.FullName}\""
                });
            else
                process = U.Run("java", new string[]
                {
                    "-jar", "apktool.jar",
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

            // if the jar doesn't exist, 0 is returned for some reason, so additional checks are required

            return outApk.Exists;
        }
    }
}
