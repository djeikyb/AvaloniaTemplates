using System;
using Avalonia;
using Avalonia.Logging;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace AvaloniaApplication1;

public static class LogStartupExtensions
{
    /// <summary>
    /// Uses Microsoft.Extensions.Logging, backed by Cysharp's ZLogger.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="guiProvider">ViewModel that stores logs.</param>
    public static AppBuilder UseZLogger(
        this AppBuilder builder,
        ObservableLogProvider? guiProvider = null
    )
    {
        // Send internal Avalonia logs to our Microsoft.Extensions.Logging provider.
        // With types expanded, it reads:
        // Avalonia.Logging.Logger.Sink = new AvaloniaApplication1.AvaloniaMelAdapter()
        Logger.Sink = new AvaloniaMelAdapter();

        // Configure Microsoft.Extensions.Logging
        var factory = LoggerFactory.Create(logging =>
        {
            // Layout logging can be verbose. Fine if going to a file. But can
            // crash a data grid. Every log added to the data grid causes a new
            // layout log!
            logging.AddFilter($"Avalonia.Area.Layout", LogLevel.Warning);

            logging.SetMinimumLevel(LogLevel.Debug);


            // var zLoggerOptions = new ZLoggerOptions
            // {
            //     InternalErrorLogger = exception => Console.Error.WriteLine(exception.ToString()),
            //     IncludeScopes = true,
            //     TimeProvider = null,
            //     FullMode = BackgroundBufferFullMode.Grow,
            //     BackgroundBufferCapacity = 0,
            //     IsFormatLogImmediatelyInStandardLog = true,
            //     CaptureThreadInfo = true
            // };
            //
            // zLoggerOptions.UseFormatter(() => new CLEFMessageTemplateFormatter());

            // logging.AddZLoggerLogProcessor( new BatchingHttpLogProcessor("http://seq.test:5341/ingest/clef", 5, zLoggerOptions));
            logging.AddZLoggerLogProcessor(options =>
            {
                options.CaptureThreadInfo = true;
                options.IncludeScopes = true;
                options.InternalErrorLogger = exception => Console.Error.WriteLine(exception.ToString());
                options.UseFormatter(() => new CLEFMessageTemplateFormatter());
                return new BatchingHttpLogProcessor("http://seq.test:5341/ingest/clef", 5, options);
            });

            logging.AddZLoggerConsole(o =>
            {
                // !!! uncomment to debug logging to seq:
                // o.IncludeScopes = true;
                // o.UseFormatter(() => new CLEFMessageTemplateFormatter());
                // return;

                o.UsePlainTextFormatter(formatter =>
                {
                    formatter.SetPrefixFormatter($"{0:timeonly} {1}]", (in MessageTemplate template, in LogInfo info) =>
                        template.Format(info.Timestamp, info.LogLevel switch
                        {
                            LogLevel.Trace => "\e[0m\e[3mTRA\e[0m",
                            LogLevel.Debug => "\e[0;90mDBG\e[0m",
                            LogLevel.Information => "\e[0;34mINF\e[0m",
                            LogLevel.Warning => "\e[0;33mWRN\e[0m",
                            LogLevel.Error => "\e[0;91mERR\e[0m",
                            LogLevel.Critical => "CRT",
                            LogLevel.None => "NON",
                            _ => "🥑"
                        }));
                });
            });

            if (guiProvider is not null) logging.AddZLoggerLogProcessor(guiProvider);
        });

        // Initialize global logger and logger factory
        Log.SetLoggerFactory(factory, "App");

        return builder;
    }
}
