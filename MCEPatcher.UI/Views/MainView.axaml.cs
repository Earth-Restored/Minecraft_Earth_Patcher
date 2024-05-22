using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MCEPatcher.UI.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MCEPatcher.UI.Views;

public partial class MainView : UserControl
{
    private MainViewModel viewModel => (MainViewModel)DataContext!;

    private string? apkFile;

    public MainView()
    {
        InitializeComponent();
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
        {
            apkFile = file.Path.LocalPath;

            if (!File.Exists(apkFile))
            {
                await MessageBoxManager.GetMessageBoxStandard("Error", "Selected file doesn't exist", icon: Icon.Error, windowStartupLocation: WindowStartupLocation.CenterOwner).ShowWindowDialogAsync(MainWindow.Instance);

                apkFile = null;
            }
        }
        else
            apkFile = null;

        viewModel.ApkFile = apkFile;
    }
}
