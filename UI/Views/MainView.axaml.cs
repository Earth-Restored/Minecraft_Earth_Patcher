using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MCEPatcher.UI.Utils;
using MCEPatcher.UI.ViewModels;
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
            await U.ShowError("Selected file doesn't exist");
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

        MainWindow.Instance.Patch(new Core.ApkProcessor.Options()
        {
            Autonomous = true,
            InApk = apkFile,
            OutApk = "Minecraft_Earth_patched.apk",
            DecodedDir = "Decoded",
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
}
