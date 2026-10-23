using Avalonia.Platform.Storage;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MCEPatcher.UI.Utils;

public static class FilePickerHelper
{
    public static async Task<string?> PickFileAsync(FilePickerOpenOptions options)
    {
        var storage = App.GetStorageProvider();
        if (storage is null) return null;

        var files = await storage.OpenFilePickerAsync(options);
        return await GetFilePathAsync(files?.FirstOrDefault());
    }

    public static async Task<string?> GetFilePathAsync(IStorageFile? file)
    {
        if (file is null) return null;

        string? path = file.TryGetLocalPath();
        if (path is not null && File.Exists(path))
        {
            return path;
        }

        var tempFile = Path.Combine(Path.GetTempPath(), file.Name);
        await using var src = await file.OpenReadAsync();
        await using var dst = File.Create(tempFile);
        await src.CopyToAsync(dst);
        return tempFile;
    }
}
