using Avalonia.Threading;
using MCEPatcher.Core;
using MCEPatcher.UI.Services;
using ReactiveUI;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MCEPatcher.UI.ViewModels;

public sealed class PatchViewModel : ViewModelBase
{
    private readonly CancellationTokenSource _cts = new();
    private PatchStepViewModel? _currentStep;
    private bool _subscribed;

    public ObservableCollection<PatchStepViewModel> Steps { get; } = new();

    public string? OutputFilePath { get; private set; }

    private bool _isApk;

    private bool _isFinished;
    public bool IsFinished
    {
        get => _isFinished;
        private set => this.RaiseAndSetIfChanged(ref _isFinished, value);
    }

    private bool _isSuccess;
    public bool IsSuccess
    {
        get => _isSuccess;
        private set
        {
            this.RaiseAndSetIfChanged(ref _isSuccess, value);
            this.RaisePropertyChanged(nameof(IsFailure));
        }
    }

    public bool IsFailure => IsFinished && !IsSuccess;

    private string _resultTitle = string.Empty;
    public string ResultTitle
    {
        get => _resultTitle;
        private set => this.RaiseAndSetIfChanged(ref _resultTitle, value);
    }

    private string _resultMessage = string.Empty;
    public string ResultMessage
    {
        get => _resultMessage;
        private set => this.RaiseAndSetIfChanged(ref _resultMessage, value);
    }

    private bool _canInstallApk;
    public bool CanInstallApk
    {
        get => _canInstallApk;
        private set => this.RaiseAndSetIfChanged(ref _canInstallApk, value);
    }

    private bool _canOpenOutputFolder;
    public bool CanOpenOutputFolder
    {
        get => _canOpenOutputFolder;
        private set => this.RaiseAndSetIfChanged(ref _canOpenOutputFolder, value);
    }

    public event Action<PatchStepViewModel>? StepActivated;

    public event Action? Finished;

    public void Start(ApkProcessor.Options options)
    {
        _isApk = true;
        RunPatch(Path.GetFullPath(options.OutApk), () => ApkProcessor.Run(options));
    }

    public void Start(IpaProcessor.Options options)
    {
        _isApk = false;
        RunPatch(Path.GetFullPath(options.OutIpa), () => IpaProcessor.Run(options));
    }

    private void RunPatch(string outputPath, Func<Task<bool>> run)
    {
        OutputFilePath = outputPath;
        Steps.Clear();
        _currentStep = null;
        IsFinished = false;

        Subscribe();

        Task.Run(async () =>
        {
            bool result;
            string? publishedLocation = null;
            try
            {
                result = await run();
                if (result)
                {
                    publishedLocation = await PlatformService.Current.PublishOutputAsync(outputPath);
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception.ToString());
                result = false;
            }

            Dispatcher.UIThread.Post(() => Finish(result, publishedLocation));
        });
    }

    private void Subscribe()
    {
        if (_subscribed)
        {
            return;
        }

        _subscribed = true;
        App.OnLogWritten += OnLogWritten;
        PatchProgress.StepStarted += OnStepStarted;
        PatchProgress.ProgressChanged += OnProgressChanged;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
        {
            return;
        }

        _subscribed = false;
        App.OnLogWritten -= OnLogWritten;
        PatchProgress.StepStarted -= OnStepStarted;
        PatchProgress.ProgressChanged -= OnProgressChanged;
    }

    private void OnStepStarted(string title)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_cts.IsCancellationRequested)
            {
                return;
            }

            StartStep(title);
        });
    }

    private void OnProgressChanged(double fraction)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_cts.IsCancellationRequested || _currentStep is null)
            {
                return;
            }

            _currentStep.Progress = fraction;
        });
    }

    private void OnLogWritten(LogEvent logEvent)
    {
        if (logEvent.Properties.ContainsKey(PatchProgress.StepMarkerProperty))
        {
            return;
        }

        var text = logEvent.RenderMessage();
        if (logEvent.Exception is not null)
        {
            text += Environment.NewLine + logEvent.Exception;
        }

        var line = new LogLine(text.TrimEnd(), logEvent.Level);

        Dispatcher.UIThread.Post(() =>
        {
            if (_cts.IsCancellationRequested)
            {
                return;
            }

            (_currentStep ?? StartStep("Preparing")).AddLog(line);
        });
    }

    private PatchStepViewModel StartStep(string title)
    {
        if (_currentStep is not null)
        {
            _currentStep.Complete(true);
            _currentStep.IsExpanded = false;
        }

        var step = new PatchStepViewModel(title);
        Steps.Add(step);
        _currentStep = step;
        StepActivated?.Invoke(step);
        return step;
    }

    private void Finish(bool success, string? publishedLocation)
    {
        if (_cts.IsCancellationRequested)
        {
            return;
        }

        Unsubscribe();

        if (_currentStep is not null)
        {
            _currentStep.Complete(success);
            // keep expanded if failed
            _currentStep.IsExpanded = !success;
        }

        IsSuccess = success;
        IsFinished = true;
        this.RaisePropertyChanged(nameof(IsFailure));

        if (success)
        {
            ResultTitle = "Patching finished";
            ResultMessage = publishedLocation is not null
                ? $"The patched {(_isApk ? "APK" : "IPA")} was saved to:\n{publishedLocation}"
                : $"The patched {(_isApk ? "APK" : "IPA")} was saved to:\n{OutputFilePath}";
            CanInstallApk = _isApk && PlatformService.Current.CanInstallApk;
            CanOpenOutputFolder = true;
        }
        else
        {
            ResultTitle = "Patching failed";
            ResultMessage = "Expand the failed step above to see what went wrong.";
            CanInstallApk = false;
            CanOpenOutputFolder = false;
        }

        Finished?.Invoke();
    }

    public string GetAllLogsText()
        => string.Join("\n\n", Steps.Select(step => $"===== {step.Title} =====\n{step.GetLogText()}"));

    public override void OnClose()
    {
        _cts.Cancel();
        Unsubscribe();
    }
}
