using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MCEPatcher.UI.Utils;
using MCEPatcher.UI.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace MCEPatcher.UI.Views;

public partial class MainView : UserControl
{
    private string? apkFile;

    public MainView()
    {
        InitializeComponent();
    }

    public async void Patch(object sender, RoutedEventArgs args)
    {
        if (apkFile is null) { await U.ShowError("Select the apk file first"); return; }
        if (!File.Exists(apkFile)) { await U.ShowError("Selected file doesn't exist"); return; }

        if (DataContext is MainViewModel viewModel)
        {
            if (viewModel.ChangeMSALoginServiceAddress && IPAddress.TryParse(viewModel.MSALoginServiceHostname.Split(':')[0], out _))
            {
                await U.ShowError("MSA login service address cannot be an IP"); 
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
    }

    public async void PickApkFile(object sender, RoutedEventArgs args)
    {
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = TopLevel.GetTopLevel(this);

        // Start async operation to open the dialog.
        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Minecraft earth APK file",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Apk")
            {
                Patterns = new[] { "*.apk" },
                MimeTypes = new[] { "application/vnd.android.package-archive" }
            } },
        });

        IStorageFile? file;
        if (files is not null && (file = files.FirstOrDefault()) is not null)
            apkFile = file.Path.LocalPath;
        else
            apkFile = null;

        if (DataContext is MainViewModel viewModel)
            viewModel.ApkFile = apkFile;
    }
}
