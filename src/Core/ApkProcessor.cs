using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MCEPatcher.Core.Services;
using Serilog;

namespace MCEPatcher.Core;

public static class ApkProcessor
{
    public static readonly string ApkHash = "BED7864C4B1BCC2774332E6D5E1BFFBA40CAB6D27782D0C3FA272F27F613A79A";

    public static readonly string ResourcePackHash = "7473B7B99FD181453D7D520903726F62F3C2433FE941EA8968E6FE589EF5A9E7";

    public static IApkToolService ToolService { get; set; } = new DesktopApkToolService();

    public static async Task<bool> Run(Options options, IApkToolService? toolService = null)
    {
        toolService ??= ToolService;
        HashSet<string> patches = [.. options.Patches];

        var inApk = new FileInfo(options.InApk);
        var outApk = new FileInfo(options.OutApk);
        var decodedDir = new DirectoryInfo(options.DecodedDir);

        if (!inApk.Exists)
        {
            Log.Fatal($"In apk '{inApk.FullName}' doesn't exist");
            return false;
        }

        Dictionary<string, string> variables = [];

        foreach (var item in options.Variables)
        {
            var split = item.Split('=');

            if (split.Length < 2)
            {
                Log.Fatal($"Variable '{item}' doesn't have value (no '=')");
                return false;
            }
            else if (split.Length > 2)
            {
                Log.Fatal($"Too many '=' in variable ({item})");
                return false;
            }

            var (name, value) = (split[0], split[1]);

            if (string.IsNullOrWhiteSpace(name))
            {
                Log.Fatal($"Name of a variable can't be empty");
                return false;
            }
            else if (variables.ContainsKey(name))
            {
                Log.Fatal($"Variable '{name}' is defined multiple times");
                return false;
            }

            variables.Add(name, value);
        }

        await toolService.EnsurePrerequisitesAsync(options);

        if (options.SkipDecode)
        {
            Log.Information("Decoding skipped");
        }
        else
        {
            PatchProgress.BeginStep("Decompiling APK");
            if (!await toolService.DecodeAsync(inApk, decodedDir))
            {
                return false;
            }

            if (!File.Exists(Path.Combine(decodedDir.FullName, "lib", "arm64-v8a", "libgenoa.so")))
            {
                Log.Error("libgenoa.so does not exist, wrong apk?");
                return false;
            }

            Log.Debug("Done");
        }

        PatchProgress.BeginStep("Applying patches");

        // does not get properly extended if not done before the hexdump and will most likely be done anyway, TODO: remove once the TODO in Patcher.PatchFile is fixed
        Log.Information($"Applying patch '{ExtendLibgenoa.Name}'");
        ExtendLibgenoa.Extent(decodedDir);
        patches.Remove(ExtendLibgenoa.Name);
        Log.Debug("Done");

        Patcher patcher = new Patcher("Patches", decodedDir.FullName);
        patcher.Patch(patches, variables, patches.Contains(ExtendLibgenoa.Name) ? new()
        {
            [ExtendLibgenoa.Name] = PatchInfo.Default
        } : new());

        if (!string.IsNullOrEmpty(options.ResourcePack))
        {
            PatchProgress.BeginStep("Applying resource pack");
            if (!await ResourcePackPatcher.Patch(options))
            {
                return false;
            }
        }

        PatchProgress.BeginStep("Updating ARCore libraries");
        await ArCoreUpdater.UpdateArCoreLibs(decodedDir);

        if (options.AndroidOSVersion >= 15)
        {
            PatchProgress.BeginStep("Patching native library page size");
            if (!await toolService.PatchPageSizeAsync(decodedDir))
            {
                return false;
            }
        }

        if (options.SkipBuild)
        {
            Log.Information($"Building skipped");
        }
        else
        {
            PatchProgress.BeginStep("Building APK");
            if (!await toolService.EncodeAsync(decodedDir, outApk))
            {
                return false;
            }

            Log.Debug("Done");

            PatchProgress.BeginStep("Aligning APK");
            if (!await toolService.AlignAsync(outApk))
            {
                return false;
            }

            Log.Debug("Done");
        }

        if (options.SkipSign)
        {
            Log.Information($"Signing skipped");
        }
        else
        {
            PatchProgress.BeginStep("Signing APK");
            if (!await toolService.SignAsync(outApk, new DirectoryInfo("Signed")))
            {
                return false;
            }

            Log.Debug("Done");
        }

        Log.Information("Finished");

        return true;
    }

    public static bool VerifyApkHash(Stream stream)
    {
        var hashBytes = SHA256.HashData(stream);

        var hash = Convert.ToHexString(hashBytes);

        return hash == ApkHash;
    }

    public static bool VerifyResourcePackHash(Stream stream)
    {
        var hashBytes = SHA256.HashData(stream);

        var hash = Convert.ToHexString(hashBytes);

        return hash == ResourcePackHash;
    }

    public sealed class Options
    {
        public required string InApk { get; init; }

        public required string OutApk { get; init; }

        public required string? ResourcePack { get; init; }

        public required string DecodedDir { get; init; }

        public required int AndroidOSVersion { get; init; }

        public required IEnumerable<string> Patches { get; init; }

        public required IEnumerable<string> Variables { get; init; }

        public bool SkipDecode { get; init; }

        public bool SkipBuild { get; init; }

        public bool SkipSign { get; init; }
    }
}
