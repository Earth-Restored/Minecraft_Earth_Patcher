using System.Diagnostics;

namespace MCEPatcher.Core;

public static class Signer
{
    public const string FileName = "uber-apk-signer.jar";

    private const string KeystoreResourceName = "MCEPatcher.Core.mcep_debug.p12";
    private const string KeystoreAlias = "mcep";
    private const string KeystorePassword = "mcepcatcher";

    public static bool Sign(FileInfo apkFile, DirectoryInfo outDir)
    {
        var tempKeystorePath = ExtractEmbeddedKeystore();

        try
        {
            Process process = ProcessRunner.Run("java", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            [
                "-jar", $"\"{Path.GetFullPath(FileName)}\"",
                "-a", $"\"{apkFile.FullName}\"",
                "-o", $"\"{outDir.FullName}\"",
                "--ks", $"\"{tempKeystorePath}\"",
                "--ksAlias", KeystoreAlias,
                "--ksPass", KeystorePassword,
                "--ksKeyPass", KeystorePassword,
                "--skipZipAlign",
            ]);

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

            outApk.MoveTo(apkFile.FullName, true);

            outDir.Delete(true);

            return true;
        }
        finally
        {
            if (File.Exists(tempKeystorePath))
            {
                try
                {
                    File.Delete(tempKeystorePath);
                }
                catch
                {
                }
            }
        }
    }

    private static string ExtractEmbeddedKeystore()
    {
        using var stream = typeof(Signer).Assembly.GetManifestResourceStream(KeystoreResourceName)
            ?? throw new FileNotFoundException($"Embedded signing keystore '{KeystoreResourceName}' could not be loaded.");

        var tempPath = Path.Combine(Path.GetTempPath(), $"mcep_debug_{Guid.NewGuid():N}.p12");
        using var fileStream = File.Create(tempPath);
        stream.CopyTo(fileStream);
        return tempPath;
    }
}
