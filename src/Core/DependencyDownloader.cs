using Serilog;

namespace MCEPatcher.Core;

public static class DependencyDownloader
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task Download(string url, string fileName)
    {
        if (File.Exists(fileName))
        {
            return;
        }

        PatchProgress.BeginStep($"Downloading {Path.GetFileName(fileName)}");
        Log.Debug($"Downloading {url}");

        var tempFileName = fileName + ".part";

        using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
        {
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            using var downloadStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(tempFileName, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[16 * 1024]; // 16KB chunks
            long totalRead = 0L;
            int bytesRead;

            var lastPercentDownloaded = -1;

            while ((bytesRead = await downloadStream.ReadAsync(buffer, 0, buffer.Length)) is not 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    var percentDownloaded = (int)((double)totalRead / totalBytes * 100);

                    if (percentDownloaded != lastPercentDownloaded)
                    {
                        PatchProgress.ReportProgress((double)totalRead / totalBytes);
                        lastPercentDownloaded = percentDownloaded;
                    }
                }
            }
        }

        File.Move(tempFileName, fileName, true);

        Log.Information($"Downloaded {Path.GetFileName(fileName)}");
    }
}
