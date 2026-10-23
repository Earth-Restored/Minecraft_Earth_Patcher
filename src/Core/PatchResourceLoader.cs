using System.Reflection;
using System.Text;

namespace MCEPatcher.Core;

public static class PatchResourceLoader
{
    private const string ResourcePrefix = "MCEPatcher.Core.Patches.";
    private static readonly Assembly CoreAssembly = typeof(PatchResourceLoader).Assembly;

    public static string? TryGetPatchContent(string? directory, string name)
    {
        if (!string.IsNullOrEmpty(directory))
        {
            string diskPath = Path.Combine(directory, $"{name}.patch");
            if (File.Exists(diskPath))
            {
                return File.ReadAllText(diskPath);
            }
        }

        string resourceName = $"{ResourcePrefix}{name}.patch";
        using var stream = CoreAssembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public static string? TryGetPatchInfoContent(string? pathOrDirectory, string name)
    {
        if (!string.IsNullOrEmpty(pathOrDirectory))
        {
            if (File.Exists(pathOrDirectory))
            {
                return File.ReadAllText(pathOrDirectory);
            }

            string diskPath = Path.Combine(pathOrDirectory, $"{name}.patch.info");
            if (File.Exists(diskPath))
            {
                return File.ReadAllText(diskPath);
            }
        }

        string resourceName = $"{ResourcePrefix}{name}.patch.info";
        using var stream = CoreAssembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
