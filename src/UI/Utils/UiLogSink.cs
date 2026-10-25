using Serilog.Core;
using Serilog.Events;
using System;

namespace MCEPatcher.UI.Utils;

public sealed class UiLogSink : ILogEventSink
{
    public event Action<LogEvent>? LogEmitted;

    public void Emit(LogEvent logEvent)
    {
        LogEmitted?.Invoke(logEvent);
    }
}
