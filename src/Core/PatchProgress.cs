using Serilog;

namespace MCEPatcher.Core;

public static class PatchProgress
{
    public const string StepMarkerProperty = "PatchStepMarker";

    public static event Action<string>? StepStarted;

    public static event Action<double>? ProgressChanged;

    public static void BeginStep(string title)
    {
        StepStarted?.Invoke(title);
        
        Log.ForContext(StepMarkerProperty, true).Information("***** {Title:l} *****", title);
    }

    public static void ReportProgress(double fraction)
    {
        ProgressChanged?.Invoke(double.Clamp(fraction, 0d, 1d));
    }
}
