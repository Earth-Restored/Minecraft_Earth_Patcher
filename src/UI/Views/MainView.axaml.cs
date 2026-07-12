using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MCEPatcher.UI.Utils;
using MCEPatcher.UI.ViewModels;
using MsBox.Avalonia;
using System.IO;
using System.Linq;
using System.Net;

namespace MCEPatcher.UI.Views;

public partial class MainView : UserControl
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
                var dialog = MessageBoxManager.GetMessageBoxStandard(
                    title: "Warning",
                    text: "The .apk file hash does not match. Patching may fail. Do you want to continue?",
                    @enum: MsBox.Avalonia.Enums.ButtonEnum.YesNo,
                    icon: MsBox.Avalonia.Enums.Icon.Error,
                    windowStartupLocation: WindowStartupLocation.CenterOwner);

                var result = await dialog.ShowWindowDialogAsync(MainWindow.Instance);

                if (result is MsBox.Avalonia.Enums.ButtonResult.No)
                {
                    return;
                }
            }
        }

        string? resourcePackFile = viewModel.ResourcePackFilePath;

        if (string.IsNullOrEmpty(resourcePackFile))
        {
            var dialog = MessageBoxManager.GetMessageBoxStandard(
                   title: "Warning",
                   text: "Resource Pack file not selected. Do you want to continue?",
                   @enum: MsBox.Avalonia.Enums.ButtonEnum.YesNo,
                   icon: MsBox.Avalonia.Enums.Icon.Warning,
                   windowStartupLocation: WindowStartupLocation.CenterOwner);

            var result = await dialog.ShowWindowDialogAsync(MainWindow.Instance);

            if (result is MsBox.Avalonia.Enums.ButtonResult.No)
            {
                return;
            }
        }
        else
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
                    var dialog = MessageBoxManager.GetMessageBoxStandard(
                        title: "Warning",
                        text: "The resource pack file hash does not match. Game may not function correctly. Do you want to continue?",
                        @enum: MsBox.Avalonia.Enums.ButtonEnum.YesNo,
                        icon: MsBox.Avalonia.Enums.Icon.Error,
                        windowStartupLocation: WindowStartupLocation.CenterOwner);

                    var result = await dialog.ShowWindowDialogAsync(MainWindow.Instance);

                    if (result is MsBox.Avalonia.Enums.ButtonResult.No)
                    {
                        return;
                    }
                }
            }
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

        MainWindow.Instance.Patch(new Core.ApkProcessor.Options()
        {
            NonInteractive = true,
            InApk = apkFile,
            OutApk = "Minecraft_Earth_patched.apk",
            ResourcePack = resourcePackFile,
            DecodedDir = Path.Combine("tmp", "apk"),
            Patches = viewModel.GetPatches(),
            Variables = viewModel.GetVariables(),
        });
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

        IStorageFile? file;
        string? filePath = files is not null && (file = files.FirstOrDefault()) is not null
            ? file.Path.LocalPath
            : null;

        if (DataContext is MainViewModel viewModel)
        {
            viewModel.ApkFile = filePath;
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

        IStorageFile? file;
        string? filePath = files is not null && (file = files.FirstOrDefault()) is not null
            ? file.Path.LocalPath
            : null;

        if (DataContext is MainViewModel viewModel)
        {
            viewModel.ResourcePackFile = filePath;
        }
    }
}
