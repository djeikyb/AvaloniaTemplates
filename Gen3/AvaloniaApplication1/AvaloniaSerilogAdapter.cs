using System;
using Avalonia.Logging;

namespace AvaloniaApplication1;

public class AvaloniaSerilogAdapter : ILogSink
{
    public bool IsEnabled(LogEventLevel level, string area)
    {
        if ("Layout".Equals(area)) return false;
        // TODO want to stop ignoring the "layout" area.. yes stdout, no datagrid cause infinite layout events
        return Serilog.Log.ForContext("area", area).IsEnabled(level switch
        {
            LogEventLevel.Verbose => Serilog.Events.LogEventLevel.Verbose,
            LogEventLevel.Debug => Serilog.Events.LogEventLevel.Debug,
            LogEventLevel.Information => Serilog.Events.LogEventLevel.Information,
            LogEventLevel.Warning => Serilog.Events.LogEventLevel.Warning,
            LogEventLevel.Error => Serilog.Events.LogEventLevel.Error,
            LogEventLevel.Fatal => Serilog.Events.LogEventLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        });
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        var serilogLevel = level switch
        {
            LogEventLevel.Verbose => Serilog.Events.LogEventLevel.Verbose,
            LogEventLevel.Debug => Serilog.Events.LogEventLevel.Debug,
            LogEventLevel.Information => Serilog.Events.LogEventLevel.Information,
            LogEventLevel.Warning => Serilog.Events.LogEventLevel.Warning,
            LogEventLevel.Error => Serilog.Events.LogEventLevel.Error,
            LogEventLevel.Fatal => Serilog.Events.LogEventLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
        Serilog.Log
            .ForContext("area", area)
            .ForContext("source", source?.GetType())
            .Write(serilogLevel, (Exception?)null, messageTemplate);
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate,
        params object?[] propertyValues)
    {
        var serilogLevel = level switch
        {
            LogEventLevel.Verbose => Serilog.Events.LogEventLevel.Verbose,
            LogEventLevel.Debug => Serilog.Events.LogEventLevel.Debug,
            LogEventLevel.Information => Serilog.Events.LogEventLevel.Information,
            LogEventLevel.Warning => Serilog.Events.LogEventLevel.Warning,
            LogEventLevel.Error => Serilog.Events.LogEventLevel.Error,
            LogEventLevel.Fatal => Serilog.Events.LogEventLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
        Serilog.Log
            .ForContext("area", area)
            .ForContext("source", source?.GetType())
            .Write(serilogLevel, (Exception?)null, messageTemplate, propertyValues);
    }
}
