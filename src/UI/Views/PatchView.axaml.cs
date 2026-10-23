using Avalonia.Controls;
using Avalonia.Interactivity;
using MCEPatcher.Core;
using MCEPatcher.UI.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using MCEPatcher.UI.Services;

namespace MCEPatcher.UI.Views;

public sealed partial class PatchView : UserControl
{
    public PatchView()
    {
        InitializeComponent();
        PatchViewModel model = new PatchViewModel();
        DataContext = model;
    }

    public PatchView(ApkProcessor.Options options) : this()
    {
        Patch(options);
    }

    public PatchView(IpaProcessor.Options options) : this()
    {
        Patch(options);
    }

    public void Patch(ApkProcessor.Options options)
    {
        (DataContext as PatchViewModel)?.Start(options, scrollViewer, chat, finishedContainer);
    }

    public void Patch(IpaProcessor.Options options)
    {
        (DataContext as PatchViewModel)?.Start(options, scrollViewer, chat, finishedContainer);
    }

    public void Back(object sender, RoutedEventArgs args)
    {
        App.NavigateBack();
    }

    public async void InstallPatchedAPK(object sender, RoutedEventArgs args)
    {
        if (DataContext is PatchViewModel model && !string.IsNullOrEmpty(model.OutputFilePath))
        {
            await PlatformService.Current.InstallApkAsync(model.OutputFilePath);
        }
    }

    public void OpenPatchedAPKLocation(object sender, RoutedEventArgs args)
    {
        string path = (DataContext as PatchViewModel)?.OutputFilePath ?? Path.GetFullPath("Solace.apk");
        OpenFileLocation(path);
    }

    public void OpenPatchedIPALocation(object sender, RoutedEventArgs args)
    {
        string path = (DataContext as PatchViewModel)?.OutputFilePath ?? Path.GetFullPath("Solace.ipa");
        OpenFileLocation(path);
    }

    private static void OpenFileLocation(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", Path.GetDirectoryName(filePath));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", $"-R \"{filePath}\"");
            }
        }
        catch { }
    }
}
