using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MCEPatcher.UI.Utils;
using MCEPatcher.UI.ViewModels;
using MsBox.Avalonia;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using MCEPatcher.UI.Services;

namespace MCEPatcher.UI.Views;

public sealed partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    public async void Patch(object sender, RoutedEventArgs args)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (viewModel.SelectedTabIndex is 1)
        {
            string? ipaFile = viewModel.IpaFilePath;

            if (ipaFile is null)
            {
                await U.ShowError("Select the IPA file first");
                return;
            }

            if (!File.Exists(ipaFile))
            {
                await U.ShowError("Selected file doesn't exist");
                return;
            }

            using (var fs = File.OpenRead(ipaFile))
            {
                if (!Core.IpaProcessor.VerifyHash(fs))
                {
                    var result = await U.ShowDialogAsync(
                        "Warning",
                        "The .ipa file hash does not match. Patching may fail. Do you want to continue?",
                        MsBox.Avalonia.Enums.Icon.Error,
                        MsBox.Avalonia.Enums.ButtonEnum.YesNo);

                    if (result is MsBox.Avalonia.Enums.ButtonResult.No)
                    {
                        return;
                    }
                }
            }

            if (viewModel.IosChangeLocatorAddress && string.IsNullOrWhiteSpace(viewModel.IosLocatorHostname))
            {
                await U.ShowError("Enter a locator hostname/IP first");
                return;
            }

            bool customAuthServer = !viewModel.IosIsSimpleMode || viewModel.IosUseCustomAuthServer;
            bool changeXalAuthAddress = customAuthServer && viewModel.IosChangeXalAuthAddress;
            bool changePlayfabApiAddress = customAuthServer && viewModel.IosChangePlayfabApiAddress;
            bool changeXboxABAddress = customAuthServer && viewModel.IosChangeXboxABAddress;
            bool changeXboxLiveAddress = customAuthServer && viewModel.IosChangeXboxLiveAddress;

            if (customAuthServer)
            {
                if (viewModel.IosLoginServerSingleDomainMode)
                {
                    if ((changeXalAuthAddress || changePlayfabApiAddress || changeXboxABAddress || changeXboxLiveAddress)
                        && string.IsNullOrWhiteSpace(viewModel.IosHostname))
                    {
                        await U.ShowError("Enter an auth server hostname/IP first");
                        return;
                    }
                }
                else
                {
                    if (changeXalAuthAddress && string.IsNullOrWhiteSpace(viewModel.IosXalHostname))
                    {
                        await U.ShowError("Enter a Xal auth hostname/IP first");
                        return;
                    }
                    if (changePlayfabApiAddress && string.IsNullOrWhiteSpace(viewModel.IosPlayfabApiHostname))
                    {
                        await U.ShowError("Enter a PlayFab API hostname/IP first");
                        return;
                    }
                    if (changeXboxABAddress && string.IsNullOrWhiteSpace(viewModel.IosXboxABHostname))
                    {
                        await U.ShowError("Enter an XboxAB hostname/IP first");
                        return;
                    }
                    if (changeXboxLiveAddress && string.IsNullOrWhiteSpace(viewModel.IosXboxLiveHostname))
                    {
                        await U.ShowError("Enter an Xbox live hostname/IP first");
                        return;
                    }
                }
            }

            var ipaOptions = new Core.IpaProcessor.Options()
            {
                Autonomous = true,
                InIpa = ipaFile,
                OutIpa = PlatformService.Current.GetDefaultOutputPath("Solace.ipa"),
                DecodedDir = "IpaDecoded",
                Protocol = viewModel.IosProtocol,
                Hostname = viewModel.IosHostname,
                LocatorProtocol = viewModel.IosLocatorProtocol,
                LocatorHostname = viewModel.IosLocatorHostname,
                AppName = viewModel.IosAppName,
                ChangeLocatorAddress = viewModel.IosChangeLocatorAddress,
                ChangeXalAuthAddress = changeXalAuthAddress,
                ForceInAppWebView = customAuthServer && viewModel.IosForceInAppWebView,
                ForceInteractiveSignIn = viewModel.IosForceInteractiveSignIn,
                ChangePlayfabApiAddress = changePlayfabApiAddress,
                ChangeXboxABAddress = changeXboxABAddress,
                ChangeXboxLiveAddress = changeXboxLiveAddress,
                DisableSunsetTimeCheck = viewModel.IosDisableSunsetTimeCheck,
                DisableSpeedLimit = viewModel.IosDisableSpeedLimit,
                ChangeAppName = viewModel.IosChangeAppName,
                RemoveDrm = viewModel.IosRemoveDrm,
            };

            if (!viewModel.IosLoginServerSingleDomainMode)
            {
                ipaOptions.XalProtocol = viewModel.IosXalProtocol;
                ipaOptions.XalHostname = viewModel.IosXalHostname;
                ipaOptions.PlayfabApiProtocol = viewModel.IosPlayfabApiProtocol;
                ipaOptions.PlayfabApiHostname = viewModel.IosPlayfabApiHostname;
                ipaOptions.XboxABProtocol = viewModel.IosXboxABProtocol;
                ipaOptions.XboxABHostname = viewModel.IosXboxABHostname;
                ipaOptions.XboxLiveProtocol = viewModel.IosXboxLiveProtocol;
                ipaOptions.XboxLiveHostname = viewModel.IosXboxLiveHostname;
            }

            PatchView view = new PatchView();
            view.Patch(ipaOptions);
            App.NavigateTo(view);
            return;
        }

        string? apkFile = viewModel.ApkFilePath;

        if (apkFile is null)
        {
            await U.ShowError("Select the apk file first");
            return;
        }

        if (!File.Exists(apkFile))
        {
            await U.ShowError("Selected apk file doesn't exist");
            return;
        }

        using (var fs = File.OpenRead(apkFile))
        {
            if (!Core.ApkProcessor.VerifyApkHash(fs))
            {
                var result = await U.ShowDialogAsync(
                    "Warning",
                    "The .apk file hash does not match. Patching may fail. Do you want to continue?",
                    MsBox.Avalonia.Enums.Icon.Error,
                    MsBox.Avalonia.Enums.ButtonEnum.YesNo);

                if (result is MsBox.Avalonia.Enums.ButtonResult.No)
                {
                    return;
                }
            }
        }

        string? resourcePackFile = viewModel.ResourcePackFilePath;

        if (!string.IsNullOrEmpty(resourcePackFile))
        {
            if (!File.Exists(resourcePackFile))
            {
                await U.ShowError("Selected resource pack file doesn't exist");
                return;
            }

            using (var fs = File.OpenRead(resourcePackFile))
            {
                if (!Core.ApkProcessor.VerifyResourcePackHash(fs))
                {
                    var result = await U.ShowDialogAsync(
                        "Warning",
                        "The resource pack file hash does not match. Game may not function correctly. Do you want to continue?",
                        MsBox.Avalonia.Enums.Icon.Error,
                        MsBox.Avalonia.Enums.ButtonEnum.YesNo);

                    if (result is MsBox.Avalonia.Enums.ButtonResult.No)
                    {
                        return;
                    }
                }
            }
        }

        if (viewModel.AndroidVersion is null or < 7)
        {
            await U.ShowError("Android version must be selected");
            return;
        }

        if (viewModel.ChangeMSALoginServiceAddress && IPAddress.TryParse(viewModel.MSALoginServiceHostname.Split(':')[0], out _))
        {
            await U.ShowError("MSA login service address cannot be an IP");
            return;
        }

        if (viewModel.ChangePlayfabApiAddress && IPAddress.TryParse(viewModel.PlayfabApiHostname.Split(':')[0], out _))
        {
            await U.ShowError("Playfab api address cannot be an IP");
            return;
        }

        if (viewModel.ChangeXboxABAddress && IPAddress.TryParse(viewModel.XboxABHostname.Split(':')[0], out _))
        {
            await U.ShowError("XboxAB address cannot be an IP");
            return;
        }

        if (viewModel.ChangeXboxLiveAddress && IPAddress.TryParse(viewModel.XboxLiveHostname.Split(':')[0], out _))
        {
            await U.ShowError("Xbox live address cannot be an IP");
            return;
        }

        if (string.IsNullOrEmpty(resourcePackFile))
        {
            var result = await U.ShowDialogAsync(
                "Info",
                "Resource Pack file not selected. Do you want to continue?",
                MsBox.Avalonia.Enums.Icon.Info,
                MsBox.Avalonia.Enums.ButtonEnum.YesNo);

            if (result is MsBox.Avalonia.Enums.ButtonResult.No)
            {
                return;
            }
        }

        var apkView = new PatchView();
        apkView.Patch(new Core.ApkProcessor.Options()
        {
            NonInteractive = true,
            InApk = apkFile,
            OutApk = PlatformService.Current.GetDefaultOutputPath("Solace.apk"),
            ResourcePack = resourcePackFile,
            DecodedDir = Path.Combine("tmp", "apk"),
            AndroidOSVersion = viewModel.AndroidVersion.Value,
            Patches = viewModel.GetPatches(),
            Variables = viewModel.GetVariables(),
        });
        App.NavigateTo(apkView);
    }

    public async void PickApkFile(object sender, RoutedEventArgs args)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Minecraft earth APK file",
            AllowMultiple = false,
            FileTypeFilter = [ new FilePickerFileType("Apk")
            {
                Patterns = ["*.apk"],
                MimeTypes = ["application/vnd.android.package-archive"]
            } ],
        });

        if (DataContext is MainViewModel viewModel)
        {
            viewModel.ApkFile = await GetFilePathAsync(files?.FirstOrDefault());
        }
    }

    public async void PickResourcePackFile(object sender, RoutedEventArgs args)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Minecraft earth resource pack file",
            AllowMultiple = false,
            FileTypeFilter = [ new FilePickerFileType("Zip")
            {
                Patterns = ["*.zip"],
                MimeTypes = ["application/zip"]
            } ],
        });

        if (DataContext is MainViewModel viewModel)
        {
            viewModel.ResourcePackFile = await GetFilePathAsync(files?.FirstOrDefault());
        }
    }

    public async void PickIpaFile(object sender, RoutedEventArgs args)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Minecraft Earth IPA file",
            AllowMultiple = false,
            FileTypeFilter = [ new FilePickerFileType("Ipa")
            {
                Patterns = ["*.ipa"],
            } ],
        });

        if (DataContext is MainViewModel viewModel)
        {
            viewModel.IpaFile = await GetFilePathAsync(files?.FirstOrDefault());
        }
    }

    private static async Task<string?> GetFilePathAsync(IStorageFile? file)
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

    public void SelectAndroid(object sender, RoutedEventArgs args)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.SelectedTabIndex = 0;
    }

    public void SelectIos(object sender, RoutedEventArgs args)
    {
        if (DataContext is MainViewModel viewModel)
            viewModel.SelectedTabIndex = 1;
    }
}
