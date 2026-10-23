using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using MCEPatcher.Core;
using ReactiveUI;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

using MCEPatcher.UI.Services;

namespace MCEPatcher.UI.ViewModels;

public sealed class PatchViewModel : ViewModelBase
{
    private ScrollViewer scrollViewer = null!;
    private StackPanel panel = null!;
    private bool patchResult;
    private readonly System.Threading.CancellationTokenSource _cts = new();

    public string? OutputFilePath { get; set; }

    private bool isApk;
    public bool IsApk
    {
        get => isApk;
        set => this.RaiseAndSetIfChanged(ref isApk, value);
    }

    private bool isIpa;
    public bool IsIpa
    {
        get => isIpa;
        set => this.RaiseAndSetIfChanged(ref isIpa, value);
    }

    private bool canInstallApk;
    public bool CanInstallApk
    {
        get => canInstallApk;
        set => this.RaiseAndSetIfChanged(ref canInstallApk, value);
    }

    private bool isApkLocationVisible;
    public bool IsApkLocationVisible
    {
        get => isApkLocationVisible;
        set => this.RaiseAndSetIfChanged(ref isApkLocationVisible, value);
    }

    public void Start(ApkProcessor.Options options, ScrollViewer _scrollViewer, StackPanel _panel, Grid finishedContainer)
    {
        RunPatch(options, _scrollViewer, _panel, finishedContainer);
    }

    public void Start(IpaProcessor.Options options, ScrollViewer _scrollViewer, StackPanel _panel, Grid finishedContainer)
    {
        RunPatch(options, _scrollViewer, _panel, finishedContainer);
    }

    private void RunPatch<T>(T options, ScrollViewer _scrollViewer, StackPanel _panel, Grid finishedContainer)
    {
        scrollViewer = _scrollViewer;
        panel = _panel;

        panel.Children.Clear();
        scrollViewer.ScrollToHome();
        finishedContainer.IsVisible = false;
        patchResult = false;

        if (options is ApkProcessor.Options apkOpts)
        {
            OutputFilePath = Path.GetFullPath(apkOpts.OutApk);
            IsApk = true;
            IsIpa = false;
            CanInstallApk = PlatformService.Current.CanInstallApk;
            IsApkLocationVisible = !PlatformService.Current.CanInstallApk;
        }
        else if (options is IpaProcessor.Options ipaOpts)
        {
            OutputFilePath = Path.GetFullPath(ipaOpts.OutIpa);
            IsApk = false;
            IsIpa = true;
            CanInstallApk = false;
            IsApkLocationVisible = false;
        }

        App.OnLogWritten += onLogWritten;

        Task task = Task.Run(async () =>
        {
            try
            {
                if (options is ApkProcessor.Options apkOpts2)
                    patchResult = await ApkProcessor.Run(apkOpts2);
                else if (options is IpaProcessor.Options ipaOpts2)
                    patchResult = await IpaProcessor.Run(ipaOpts2);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                patchResult = false;
            }
        });
        task.ContinueWith(t =>
        {
            if (_cts.IsCancellationRequested) return;

            Dispatcher.UIThread.Post(() =>
            {
                if (_cts.IsCancellationRequested) return;

                finishedContainer.IsVisible = true;
                scrollViewer.ScrollToEnd();
                if (!patchResult)
                {
                    CanInstallApk = false;
                    IsApkLocationVisible = false;
                    for (int i = 1; i < finishedContainer.Children.Count; i++)
                        finishedContainer.Children[i].IsVisible = false;
                }
            });
        });
    }

    private void onLogWritten(string? text)
    {
        if (_cts.IsCancellationRequested) return;

        Dispatcher.UIThread.Post(() =>
        {
            if (_cts.IsCancellationRequested) return;

            SelectableTextBlock block = new SelectableTextBlock()
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness(0, 0, 0, 2)
            };
            panel.Children.Add(block);
            scrollViewer.ScrollToEnd();
        });
    }

    public override void OnClose()
    {
        _cts.Cancel();
        App.OnLogWritten -= onLogWritten;
    }
}
