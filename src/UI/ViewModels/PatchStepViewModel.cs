using ReactiveUI;
using Serilog.Events;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace MCEPatcher.UI.ViewModels;

public enum PatchStepState
{
    Running,
    Succeeded,
    Failed,
}

public readonly record struct LogLine(string Text, LogEventLevel Level)
{
    public bool IsError => Level >= LogEventLevel.Error;

    public bool IsWarning => Level == LogEventLevel.Warning;

    public bool IsVerbose => Level <= LogEventLevel.Debug;
}

public sealed class PatchStepViewModel : ReactiveObject
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public PatchStepViewModel(string title)
    {
        Title = title;
    }

    public string Title { get; }

    public ObservableCollection<LogLine> Logs { get; } = new();

    public bool HasLogs => Logs.Count is not 0;

    private PatchStepState state = PatchStepState.Running;
    public PatchStepState State
    {
        get => state;
        private set
        {
            this.RaiseAndSetIfChanged(ref state, value);
            this.RaisePropertyChanged(nameof(IsRunning));
            this.RaisePropertyChanged(nameof(IsSucceeded));
            this.RaisePropertyChanged(nameof(IsFailed));
            this.RaisePropertyChanged(nameof(StatusText));
        }
    }

    public bool IsRunning => State == PatchStepState.Running;

    public bool IsSucceeded => State == PatchStepState.Succeeded;

    public bool IsFailed => State == PatchStepState.Failed;

    private bool isExpanded = true;
    public bool IsExpanded
    {
        get => isExpanded;
        set => this.RaiseAndSetIfChanged(ref isExpanded, value);
    }

    private double? progress;

    public double? Progress
    {
        get => progress;
        set
        {
            this.RaiseAndSetIfChanged(ref progress, value);
            this.RaisePropertyChanged(nameof(HasProgress));
            this.RaisePropertyChanged(nameof(ProgressPercent));
            this.RaisePropertyChanged(nameof(StatusText));
        }
    }

    public bool HasProgress => Progress is not null;

    public double ProgressPercent => (Progress ?? 0d) * 100d;

    private string duration = string.Empty;

    public string StatusText => State switch
    {
        PatchStepState.Running when Progress is { } value => $"{value:P0}",
        PatchStepState.Running => string.Empty,
        _ => duration,
    };

    public void AddLog(LogLine line)
    {
        Logs.Add(line);

        if (Logs.Count is 1)
        {
            this.RaisePropertyChanged(nameof(HasLogs));
        }
    }

    public void Complete(bool success)
    {
        if (State is not PatchStepState.Running)
        {
            return;
        }

        _stopwatch.Stop();
        var seconds = _stopwatch.Elapsed.TotalSeconds;
        duration = seconds < 60 ? $"{seconds:0.0} s" : $"{(int)seconds / 60} min {(int)seconds % 60} s";

        State = success ? PatchStepState.Succeeded : PatchStepState.Failed;
    }

    public string GetLogText()
        => string.Join('\n', Logs.Select(line => line.Text));
}
