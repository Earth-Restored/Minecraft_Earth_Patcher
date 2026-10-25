using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using AndroidX.Core.Content;
using MCEPatcher.Core;
using MCEPatcher.UI.Services;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using AndroidEnvironment = Android.OS.Environment;

namespace MCEPatcher.UI.Android;

public sealed class AndroidPlatformService : IPlatformService
{
    private readonly Context _context;

    public AndroidPlatformService(Context context)
    {
        _context = context.ApplicationContext ?? context;
    }

    public int? GetDefaultAndroidVersion()
    {
        if (!string.IsNullOrEmpty(Build.VERSION.Release))
        {
            var parts = Build.VERSION.Release.Split('.');
            if (int.TryParse(parts[0], out int releaseVer) && releaseVer >= 7)
            {
                return releaseVer;
            }
        }

        return (int)Build.VERSION.SdkInt switch
        {
            >= 35 => 15,
            34 => 14,
            33 => 13,
            >= 31 => 12,
            30 => 11,
            29 => 10,
            28 => 9,
            >= 26 => 8,
            _ => 7
        };
    }

    public bool CanInstallApk => true;

    public Task<bool> InstallApkAsync(string apkPath)
    {
        try
        {
            var javaFile = new Java.IO.File(apkPath);
            if (!javaFile.Exists())
            {
                return Task.FromResult(false);
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                if (!_context.PackageManager!.CanRequestPackageInstalls())
                {
                    var settingsIntent = new Intent(Settings.ActionManageUnknownAppSources);
                    settingsIntent.SetData(global::Android.Net.Uri.Parse($"package:{_context.PackageName}"));
                    settingsIntent.SetFlags(ActivityFlags.NewTask);
                    _context.StartActivity(settingsIntent);
                    return Task.FromResult(false);
                }
            }

            var apkUri = FileProvider.GetUriForFile(
                _context,
                $"{_context.PackageName}.fileprovider",
                javaFile);

            var installIntent = new Intent(Intent.ActionView);
            installIntent.SetDataAndType(apkUri, "application/vnd.android.package-archive");
            installIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);
            _context.StartActivity(installIntent);

            return Task.FromResult(true);
        }
        catch (Exception exception)
        {
            Log.Error($"Failed to trigger APK install: {exception}");
            return Task.FromResult(false);
        }
    }

    public string GetDefaultOutputPath(string defaultFileName)
    {
        var extDir = _context.GetExternalFilesDir(null)?.AbsolutePath
            ?? _context.FilesDir?.AbsolutePath
            ?? AndroidEnvironment.ExternalStorageDirectory?.AbsolutePath
            ?? "/sdcard/Download";

        return Path.Combine(extDir, defaultFileName);
    }

    public Task<string?> PublishOutputAsync(string outputPath)
    {
        return Task.Run(() =>
        {
            if (!File.Exists(outputPath))
            {
                return null;
            }

            PatchProgress.BeginStep("Saving to Downloads");

            try
            {
                var location = Build.VERSION.SdkInt >= BuildVersionCodes.Q
                    ? PublishWithMediaStore(outputPath)
                    : PublishWithFileApi(outputPath);

                if (location is not null)
                {
                    Log.Information($"Saved to {location}");
                }

                return location;
            }
            catch (Exception exception)
            {
                Log.Warning($"Failed to save the output to Downloads, it is still available at '{outputPath}': {exception}");
                return null;
            }
        });
    }

    private string? PublishWithMediaStore(string outputPath)
    {
        var resolver = _context.ContentResolver!;
        var collection = MediaStore.Downloads.ExternalContentUri!;
        var fileName = Path.GetFileName(outputPath);
        var relativePath = AndroidEnvironment.DirectoryDownloads + "/";

        global::Android.Net.Uri? target = null;
        using (var cursor = resolver.Query(
            collection,
            [IBaseColumns.Id],
            $"{MediaStore.IMediaColumns.DisplayName} = ? AND {MediaStore.IMediaColumns.RelativePath} = ?",
            [fileName, relativePath],
            null))
        {
            if (cursor is not null && cursor.MoveToFirst())
            {
                target = ContentUris.WithAppendedId(collection, cursor.GetLong(0));
            }
        }

        var pendingValues = new ContentValues();
        pendingValues.Put(MediaStore.IMediaColumns.IsPending, 1);

        if (target is null)
        {
            pendingValues.Put(MediaStore.IMediaColumns.DisplayName, fileName);
            pendingValues.Put(MediaStore.IMediaColumns.MimeType, GetMimeType(fileName));
            pendingValues.Put(MediaStore.IMediaColumns.RelativePath, relativePath);
            target = resolver.Insert(collection, pendingValues) ?? throw new IOException("Failed to create the Downloads entry");
        }
        else
        {
            resolver.Update(target, pendingValues, null, null);
        }

        using (var output = resolver.OpenOutputStream(target, "wt") ?? throw new IOException("Failed to open the Downloads entry"))
        using (var input = File.OpenRead(outputPath))
        {
            CopyWithProgress(input, output);
        }

        var doneValues = new ContentValues();
        doneValues.Put(MediaStore.IMediaColumns.IsPending, 0);
        resolver.Update(target, doneValues, null, null);

        string actualName = fileName;
        using (var cursor = resolver.Query(target, [MediaStore.IMediaColumns.DisplayName], null, null, null))
        {
            if (cursor is not null && cursor.MoveToFirst())
            {
                actualName = cursor.GetString(0) ?? fileName;
            }
        }

        return $"Download/{actualName}";
    }

    private string? PublishWithFileApi(string outputPath)
    {
        if (ContextCompat.CheckSelfPermission(_context, global::Android.Manifest.Permission.WriteExternalStorage) != Permission.Granted)
        {
            Log.Warning($"Storage permission isn't granted, the output is available at '{outputPath}'");
            return null;
        }

        var downloadsDir = AndroidEnvironment.GetExternalStoragePublicDirectory(AndroidEnvironment.DirectoryDownloads)!.AbsolutePath;
        Directory.CreateDirectory(downloadsDir);
        var targetPath = Path.Combine(downloadsDir, Path.GetFileName(outputPath));

        using (var input = File.OpenRead(outputPath))
        using (var output = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            CopyWithProgress(input, output);
        }

        return targetPath;
    }

    private static void CopyWithProgress(Stream input, Stream output)
    {
        var total = input.Length;
        var copied = 0L;
        var lastPercent = -1;
        byte[] buffer = new byte[81920];
        int read;
        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            output.Write(buffer, 0, read);
            copied += read;

            if (total > 0)
            {
                var percent = (int)(copied * 100 / total);
                if (percent != lastPercent)
                {
                    lastPercent = percent;
                    PatchProgress.ReportProgress((double)copied / total);
                }
            }
        }
    }

    private static string GetMimeType(string fileName)
        => Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".apk" => "application/vnd.android.package-archive",
            _ => "application/octet-stream",
        };

    public Task OpenOutputFolderAsync(string outputPath)
    {
        try
        {
            var intent = new Intent(DownloadManager.ActionViewDownloads);
            intent.SetFlags(ActivityFlags.NewTask);
            _context.StartActivity(intent);
        }
        catch (Exception exception)
        {
            Log.Warning($"Failed to open the Downloads folder: {exception.Message}");
        }

        return Task.CompletedTask;
    }
}
