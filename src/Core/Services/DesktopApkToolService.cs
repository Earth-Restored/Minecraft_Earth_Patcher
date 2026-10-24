using Serilog;

namespace MCEPatcher.Core.Services;

public sealed class DesktopApkToolService : IApkToolService
{
    public async Task EnsurePrerequisitesAsync(ApkProcessor.Options options, CancellationToken cancellationToken = default)
    {
        if (!options.SkipDecode || !options.SkipBuild)
        {
            await DependencyDownloader.Download("https://github.com/iBotPeaches/Apktool/releases/download/v3.0.3/apktool_3.0.3.jar", APK.FileName);
            await DependencyDownloader.Download(BuildTools.DownloadUrl, BuildTools.FileName);
        }

        if (options.AndroidOSVersion >= 15)
        {
            await DependencyDownloader.Download(PatchElf.DownloadUrl, PatchElf.FileName);
        }

        await DependencyDownloader.Download(ArCoreUpdater.DownloadUrl, ArCoreUpdater.FileName);

        if (!options.SkipSign)
        {
            await DependencyDownloader.Download("https://github.com/patrickfav/uber-apk-signer/releases/download/v1.3.0/uber-apk-signer-1.3.0.jar", Signer.FileName);
        }
    }

    public Task<bool> DecodeAsync(FileInfo inApk, DirectoryInfo decodedDir, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(APK.Decode(inApk, decodedDir));
    }

    public Task<bool> EncodeAsync(DirectoryInfo decodedDir, FileInfo outApk, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(APK.Encode(decodedDir, outApk));
    }

    public Task<bool> AlignAsync(FileInfo apkFile, CancellationToken cancellationToken = default)
    {
        return BuildTools.AlignAsync(apkFile, cancellationToken);
    }

    public Task<bool> SignAsync(FileInfo apkFile, DirectoryInfo outDir, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Signer.Sign(apkFile, outDir));
    }

    public Task<bool> PatchPageSizeAsync(DirectoryInfo decodedDir, CancellationToken cancellationToken = default)
    {
        return PatchElf.PatchPageSizeAsync(decodedDir, Log.Logger, cancellationToken);
    }
}
