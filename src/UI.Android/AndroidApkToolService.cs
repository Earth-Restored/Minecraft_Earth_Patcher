using Android.Content;
using Java.Lang;
using MCEPatcher.Core;
using MCEPatcher.Core.Services;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MCEPatcher.UI.Android;

public sealed class AndroidApkToolService : IApkToolService
{
    private readonly Context _context;
    private readonly string _nativeLibraryDir;
    private readonly string _cacheDir;
    private readonly string _frameworkDir;
    private string _aaptPath;

    private readonly Class _bridgeClass;
    private readonly Class _callbackClass;
    private readonly Class _stringClass;

    public AndroidApkToolService(Context context)
    {
        _context = context.ApplicationContext ?? context;
        _nativeLibraryDir = _context.ApplicationInfo?.NativeLibraryDir ?? string.Empty;
        _cacheDir = _context.CacheDir?.AbsolutePath ?? Path.GetTempPath();
        _frameworkDir = Path.Combine(_cacheDir, "framework");
        _aaptPath = Path.Combine(_nativeLibraryDir, "libaapt2.so");

        try
        {
            Directory.SetCurrentDirectory(_cacheDir);
        }
        catch (System.Exception exception)
        {
            Log.Warning("Could not set current directory to cache dir: {Exception}", exception);
        }

        var classLoader = _context.ClassLoader ?? Class.FromType(typeof(AndroidApkToolService)).ClassLoader!;
        _bridgeClass = Class.ForName("com.github.earth_restored.mcepcatcher.bridge.PatcherBridge", true, classLoader);
        _callbackClass = Class.ForName("com.github.earth_restored.mcepcatcher.bridge.LogCallback", true, classLoader);
        _stringClass = Class.FromType(typeof(Java.Lang.String))!;

        EnsureAaptExecutable();
    }

    private void EnsureAaptExecutable()
    {
        try
        {
            if (File.Exists(_aaptPath))
            {
                Log.Information("Using bundled aapt2 at {AaptPath}", _aaptPath);
                return;
            }

            var fallbackAapt2 = Path.Combine(_context.ApplicationInfo?.NativeLibraryDir ?? string.Empty, "libaapt2.so");
            if (File.Exists(fallbackAapt2))
            {
                _aaptPath = fallbackAapt2;
                Log.Information("Using fallback aapt2 at {AaptPath}", _aaptPath);
                return;
            }

            var fallbackAapt = Path.Combine(_context.ApplicationInfo?.NativeLibraryDir ?? string.Empty, "libaapt.so");
            if (File.Exists(fallbackAapt))
            {
                _aaptPath = fallbackAapt;
                Log.Information("Using fallback aapt at {AaptPath}", _aaptPath);
                return;
            }

            Log.Warning("Bundled libaapt2.so not found at {AaptPath}", _aaptPath);
        }
        catch (System.Exception exception)
        {
            Log.Warning("Failed to locate aapt executable: {Exception}", exception);
        }
    }

    public async Task EnsurePrerequisitesAsync(ApkProcessor.Options options, CancellationToken cancellationToken = default)
    {
        EnsureAaptExecutable();

        if (!File.Exists(_aaptPath))
        {
            throw new FileNotFoundException($"Required tool 'libaapt.so' not found in {_nativeLibraryDir}");
        }

        Directory.CreateDirectory(_frameworkDir);

        ArCoreUpdater.FileName = Path.Combine(_cacheDir, $"arcore-1.50.0.zip");
        await DependencyDownloader.Download(ArCoreUpdater.DownloadUrl, ArCoreUpdater.FileName);
    }

    public Task<bool> DecodeAsync(FileInfo inputApk, DirectoryInfo decodedDir, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                if (_context.ClassLoader is not null)
                {
                    Java.Lang.Thread.CurrentThread().ContextClassLoader = _context.ClassLoader;
                }

                if (decodedDir.Exists)
                {
                    decodedDir.Delete(true);
                }

                decodedDir.Create();

                var decompileMethod = _bridgeClass.GetMethod(
                    "decompile",
                    _stringClass,
                    _stringClass,
                    _stringClass,
                    _callbackClass);

                var callback = CreateLogCallback(_callbackClass, msg => Log.Information("[Apktool] {Message}", msg));

                var result = decompileMethod.Invoke(
                    null,
                    new Java.Lang.String(inputApk.FullName),
                    new Java.Lang.String(decodedDir.FullName),
                    new Java.Lang.String(_frameworkDir),
                    callback);

                if (result is Java.Lang.Boolean boolResult)
                {
                    return boolResult.BooleanValue();
                }

                return false;
            }
            catch (System.Exception exception)
            {
                Log.Error("Exception during Apktool decode: {Exception}", exception);
                return false;
            }
        }, cancellationToken);
    }

    public Task<bool> EncodeAsync(DirectoryInfo decodedDir, FileInfo outApk, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                if (_context.ClassLoader is not null)
                {
                    Java.Lang.Thread.CurrentThread().ContextClassLoader = _context.ClassLoader;
                }

                if (outApk.Exists)
                {
                    outApk.Delete();
                }

                var recompileMethod = _bridgeClass.GetMethod(
                    "recompile",
                    _stringClass,
                    _stringClass,
                    _stringClass,
                    _stringClass,
                    _callbackClass);

                var callback = CreateLogCallback(_callbackClass, msg => Log.Information("[Apktool] {Message}", msg));

                var result = recompileMethod.Invoke(
                    null,
                    new Java.Lang.String(decodedDir.FullName),
                    new Java.Lang.String(outApk.FullName),
                    new Java.Lang.String(_aaptPath),
                    new Java.Lang.String(_frameworkDir),
                    callback);

                if (result is Java.Lang.Boolean boolResult)
                {
                    return boolResult.BooleanValue();
                }

                return false;
            }
            catch (System.Exception exception)
            {
                Log.Error("Exception during Apktool encode: {Exception}", exception);
                return false;
            }
        }, cancellationToken);
    }

    public Task<bool> AlignAsync(FileInfo apkFile, CancellationToken cancellationToken = default)
    {
        Log.Information("Zip-align will be handled by ApkSigner during signing.");
        return Task.FromResult(true);
    }

    public Task<bool> SignAsync(FileInfo apkFile, DirectoryInfo outDir, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            try
            {
                if (_context.ClassLoader is not null)
                {
                    Java.Lang.Thread.CurrentThread().ContextClassLoader = _context.ClassLoader;
                }

                if (!outDir.Exists)
                {
                    outDir.Create();
                }

                var signedApkPath = Path.Combine(outDir.FullName, apkFile.Name);

                if (File.Exists(signedApkPath))
                {
                    File.Delete(signedApkPath);
                }

                var signMethod = _bridgeClass.GetMethod(
                    "sign",
                    _stringClass,
                    _stringClass,
                    _stringClass,
                    _stringClass,
                    _stringClass,
                    _callbackClass);

                var callback = CreateLogCallback(_callbackClass, msg => Log.Information("[ApkSigner] {Message}", msg));

                var result = signMethod.Invoke(
                    null,
                    new Java.Lang.String(apkFile.FullName),
                    new Java.Lang.String(signedApkPath),
                    null!,
                    null!,
                    null!,
                    callback);

                var success = (result is Java.Lang.Boolean boolResult) && boolResult.BooleanValue() && File.Exists(signedApkPath);
                if (!success)
                {
                    return false;
                }

                File.Move(signedApkPath, apkFile.FullName, true);

                try
                {
                    outDir.Delete(true);
                }
                catch
                {
                }

                return true;
            }
            catch (System.Exception exception)
            {
                Log.Error("Exception during APK signing: {Exception}", exception);
                return false;
            }
        }, cancellationToken);
    }

    public Task<bool> PatchPageSizeAsync(DirectoryInfo decodedDir, CancellationToken cancellationToken = default)
    {
        Log.Information("Skipping ELF 16KB page-size patch on Android device.");
        return Task.FromResult(true);
    }

    private static Java.Lang.Object CreateLogCallback(Class callbackClass, Action<string> logAction)
    {
        var handler = new JavaLogCallbackHandler(logAction);
        return Java.Lang.Reflect.Proxy.NewProxyInstance(
            callbackClass.ClassLoader,
            [callbackClass],
            handler);
    }

    private sealed class JavaLogCallbackHandler : Java.Lang.Object, Java.Lang.Reflect.IInvocationHandler
    {
        private readonly Action<string> _logAction;

        public JavaLogCallbackHandler(Action<string> logAction)
        {
            _logAction = logAction;
        }

        public Java.Lang.Object? Invoke(Java.Lang.Object? proxy, Java.Lang.Reflect.Method? method, Java.Lang.Object[]? args)
        {
            if (method?.Name is "onLog" && args is [var message, ..])
            {
                _logAction(message.ToString() ?? string.Empty);
            }

            return null;
        }
    }
}
