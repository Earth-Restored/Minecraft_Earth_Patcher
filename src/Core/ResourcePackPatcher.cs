using System.IO.Compression;
using System.Text;
using System.Text.Json;
using BitcoderCZ.IO;
using Serilog;

namespace MCEPatcher.Core;

internal static class ResourcePackPatcher
{

    public static async Task<bool> Patch(ApkProcessor.Options options)
    {
        if (!File.Exists(options.ResourcePack))
        {
            Log.Fatal($"Resource pack file '{options.ResourcePack}' does not exist");
            return false;
        }

        var resourcePackFolder = new RelativeDirectory("tmp").ToAbsolute() / "resourcepack";

        try
        {
            Log.Information("Extracting resource pack");

            using (var resourcePackFS = File.OpenRead(options.ResourcePack))
            using (var resourcePackZip = new ZipArchive(resourcePackFS, ZipArchiveMode.Read))
            {
                var genoaEntry = resourcePackZip.GetEntry("genoa.mcpack");

                if (genoaEntry is null)
                {
                    Log.Fatal("Invalid resourcepack file, 'genoa.mcpack' missing");
                    return false;
                }

                using (var genoaStream = await genoaEntry.OpenAsync())
                using (var genoaZip = new ZipArchive(genoaStream, ZipArchiveMode.Read))
                {
                    await genoaZip.ExtractToDirectoryAsync(resourcePackFolder.Value, overwriteFiles: true);
                }
            }

            Log.Debug("Done");

            Log.Information("Copying resource pack files");

            (resourcePackFolder / new RelativeFile("manifest.json")).Delete(); // So it doesn't overwrite the existing one in genoa_base

            var resourcePackRoot = new AbsoluteDirectory(Path.GetFullPath(options.DecodedDir)) / "assets" / "resource_packs" / "genoa_base";
            Log.Information(resourcePackFolder.Value + " " + resourcePackRoot.Value);
            resourcePackFolder.CopyContentsTo(resourcePackRoot, overwrite: true, recursive: true);

            Log.Debug("Done");

            Log.Information("Patching resource pack json files");

            Log.Information("Generating _ui_defs.json");

            List<string> contents = [];
            var uiDirectory = resourcePackRoot / "ui";
            foreach (var file in uiDirectory.EnumerateFiles(SearchOption.AllDirectories))
            {
                if (file.ExtensionSpan.Equals(".json", StringComparison.OrdinalIgnoreCase) &&
                    file.NameSpan is not "_ui_defs.json")
                {
                    var relativePath = file.RelativeTo(resourcePackRoot).Value.Replace('\\', '/');

                    if (relativePath.StartsWith('/'))
                    {
                        relativePath = relativePath[1..];
                    }

                    if (relativePath.StartsWith("ui/animation/", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    contents.Add(relativePath);
                }
            }

            File.WriteAllText(
                (resourcePackRoot / "ui" / "_ui_defs.json").Value,
                JsonSerializer.Serialize(new Dictionary<string, IEnumerable<string>> { { "ui_defs", contents.Order() } }, CoreJsonSerializerContext.Default.DictionaryStringIEnumerableString),
                Encoding.UTF8);

            Log.Debug("Done");

            Log.Information("Generating textures_list.json");

            var textureTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".tga", ".tiff", ".png", ".jpg"
                };
            contents.Clear();

            foreach (var file in (resourcePackRoot / "textures").EnumerateFiles(SearchOption.AllDirectories))
            {
                var extension = file.Extension;

                if (textureTypes.Contains(extension))
                {
                    var fileWithoutExtension = file.Parent! / new RelativeFile(file.NameWithoutExtension);

                    var relativePath = fileWithoutExtension.RelativeTo(resourcePackRoot).Value.Replace('\\', '/');

                    if (relativePath.StartsWith('/'))
                    {
                        relativePath = relativePath[1..];
                    }

                    contents.Add(relativePath);
                }
            }

            File.WriteAllText(
                (resourcePackRoot / "textures" / "textures_list.json").Value,
                JsonSerializer.Serialize(contents, CoreJsonSerializerContext.Default.ListString),
                Encoding.UTF8);

            Log.Debug("Done");

            Log.Information("Generating contents.json");

            var contents2 = new List<Dictionary<string, string>>();

            foreach (var file in resourcePackRoot.EnumerateFiles(SearchOption.AllDirectories))
            {
                var relativePath = file.RelativeTo(resourcePackRoot).Value.Replace("\\", "/");

                if (relativePath.StartsWith('/'))
                {
                    relativePath = relativePath[1..];
                }

                contents2.Add(new Dictionary<string, string>
                    {
                        { "path", relativePath }
                    });
            }

            File.WriteAllText(
                (resourcePackRoot / "contents.json").Value,
                JsonSerializer.Serialize(new Dictionary<string, IEnumerable<Dictionary<string, string>>> { { "content", contents2.OrderBy(item => item.First().Key) } }, CoreJsonSerializerContext.Default.DictionaryStringIEnumerableDictionaryStringString),
                Encoding.UTF8);

            Log.Debug("Done");

            Log.Debug("Done");

            return true;
        }
        catch (Exception exception)
        {
            Log.Fatal($"Failed to patch resource pack: {exception}");
            return false;
        }
        finally
        {
            resourcePackFolder.Delete(true);
        }
    }
}