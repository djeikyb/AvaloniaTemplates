using System;
using Avalonia;
using ObservableCollections;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace AvaloniaApplication1;

public static class ObservableLogEventSinkExtensions
{
    public static LoggerConfiguration ObservableSink(
        this LoggerSinkConfiguration loggerConfiguration,
        ObservableLogEventSink logsSink
    ) => loggerConfiguration.Sink(logsSink);

    public static AppBuilder UseSerilog(this AppBuilder builder, ObservableLogEventSink sink)
    {
        Avalonia.Logging.Logger.Sink = new AvaloniaSerilogAdapter();
        return builder.AfterSetup((Action<AppBuilder>)(_ =>
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Debug()
                .WriteTo.ObservableSink(sink)
                .WriteTo.Console()
                .CreateLogger()));
    }
}

public class ObservableLogEventSink(int capacity) : ILogEventSink
{
    public ObservableFixedSizeRingBuffer<LogEvent> Logs { get; } = new(capacity);
    public void Emit(LogEvent logEvent) => Logs.AddLast(logEvent);
}
