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
            Process process = U.Run(new FileInfo(apkToolName), new string[]
            {
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
            Process process = U.Run(new FileInfo(apkToolName), new string[]
            {
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
