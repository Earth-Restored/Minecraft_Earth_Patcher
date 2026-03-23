using System.Diagnostics;

namespace MCEPatcher.Core
{
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

            if (exitCode != 0) return false;

            FileInfo? outApk = outDir.EnumerateFiles().Where(info => info.Extension == ".apk").FirstOrDefault();
            if (outApk is null) return false;

            apkFile.Delete();

            outApk.MoveTo(apkFile.FullName);

            outDir.Delete(true);

            return true;
        }
    }
}
