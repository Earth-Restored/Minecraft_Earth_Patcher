using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MCEPatcher.Core.Services;

public interface IApkToolService
{
    Task EnsurePrerequisitesAsync(ApkProcessor.Options options, CancellationToken cancellationToken = default);

    Task<bool> DecodeAsync(FileInfo inputApk, DirectoryInfo decodedDir, CancellationToken cancellationToken = default);

    Task<bool> EncodeAsync(DirectoryInfo decodedDir, FileInfo outputApk, CancellationToken cancellationToken = default);

    Task<bool> AlignAsync(FileInfo apkFile, CancellationToken cancellationToken = default);

    Task<bool> SignAsync(FileInfo apkFile, DirectoryInfo outputDir, CancellationToken cancellationToken = default);

    Task<bool> PatchPageSizeAsync(DirectoryInfo decodedDir, CancellationToken cancellationToken = default);
}
