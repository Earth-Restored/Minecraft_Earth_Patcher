using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MCEPatcher.Core;
using MCEPatcher.UI.Services;
using MCEPatcher.UI.ViewModels;

namespace MCEPatcher.UI.Views;

public sealed partial class PatchView : UserControl
{
    private const double StickToBottomThreshold = 24;

    public PatchView()
    {
        InitializeComponent();
        PatchViewModel model = new PatchViewModel();
        model.Finished += OnFinished;
        DataContext = model;

        AddHandler(ScrollViewer.ScrollChangedEvent, OnScrollChanged);
    }

    public PatchView(ApkProcessor.Options options)
        : this()
    {
        Patch(options);
    }

    public PatchView(IpaProcessor.Options options)
        : this()
    {
        Patch(options);
    }

    private PatchViewModel? Model => DataContext as PatchViewModel;

    public void Patch(ApkProcessor.Options options)
    {
        Model?.Start(options);
    }

    public void Patch(IpaProcessor.Options options)
    {
        Model?.Start(options);
    }

    private static void OnScrollChanged(object? sender, ScrollChangedEventArgs args)
    {
        if (args.Source is not ScrollViewer scrollViewer || !scrollViewer.Classes.Contains("autoscroll"))
        {
            return;
        }

        if (args.ExtentDelta.Y <= 0)
        {
            return;
        }

        var previousExtent = scrollViewer.Extent.Height - args.ExtentDelta.Y;
        var previousBottom = scrollViewer.Offset.Y - args.OffsetDelta.Y + scrollViewer.Viewport.Height;
        if (previousExtent - previousBottom <= StickToBottomThreshold)
        {
            scrollViewer.ScrollToEnd();
        }
    }

    private void OnFinished()
        => Dispatcher.UIThread.Post(resultCard.BringIntoView, DispatcherPriority.Background);

    public void Back(object sender, RoutedEventArgs args)
        => App.NavigateBack();

    public async void InstallPatchedApk(object sender, RoutedEventArgs args)
    {
        if (Model?.OutputFilePath is { Length: > 0 } path)
        {
            await PlatformService.Current.InstallApkAsync(path);
        }
    }

    public async void OpenOutputFolder(object sender, RoutedEventArgs args)
    {
        if (Model?.OutputFilePath is { Length: > 0 } path)
        {
            await PlatformService.Current.OpenOutputFolderAsync(path);
        }
    }

    public async void CopyStepLog(object sender, RoutedEventArgs args)
    {
        if ((sender as Control)?.DataContext is PatchStepViewModel step)
        {
            await CopyToClipboard(step.GetLogText());
        }
    }

    public async void CopyAllLogs(object sender, RoutedEventArgs args)
    {
        if (Model is { } model)
        {
            await CopyToClipboard(model.GetAllLogsText());
        }
    }

    private async System.Threading.Tasks.Task CopyToClipboard(string text)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(text);
        }
    }
}
