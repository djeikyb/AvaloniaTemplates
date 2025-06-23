// https://raw.githubusercontent.com/Cysharp/ZLogger/refs/tags/2.5.10/sandbox/ConsoleApp/SampleCustomFormatter.cs

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;
using ZLogger.Providers;

namespace AvaloniaApplication1.Logging;

public class AppLoggerOptions : ZLoggerOptions
{
    public (string Key, string Value)[]? StaticLogProps { get; set; }
}

[ProviderAlias("Seq")]
public class SeqLoggerProvider(ZLoggerLogProcessorLoggerProvider provider)
    : ILoggerProvider, ISupportExternalScope, IAsyncDisposable
{
    public ILogger CreateLogger(string categoryName) => provider.CreateLogger(categoryName);
    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => provider.SetScopeProvider(scopeProvider);
    public void Dispose() => provider.Dispose();
    public async ValueTask DisposeAsync() => await provider.DisposeAsync();
}

public static class SeqZloggerExtensions
{
    public static ILoggingBuilder AddSeq(
        this ILoggingBuilder builder,
        string host,
        Action<AppLoggerOptions> configure)
    {
        var ub = new UriBuilder(host);
        ub.Path = "ingest/clef";

        Func<AppLoggerOptions, IAsyncLogProcessor> logProcessorFactory;
        logProcessorFactory = options =>
        {
            options.InternalErrorLogger = exception => Console.Error.WriteLine(exception.ToString());
            options.UseFormatter(() => new CLEFMessageTemplateFormatter(options));

            configure(options);

            return new BatchingHttpLogProcessor(ub.Uri.ToString(), 5, options);
        };

        builder.Services.AddSingleton<ILoggerProvider, SeqLoggerProvider>(_ =>
        {
            AppLoggerOptions options = new();
            return new SeqLoggerProvider(new ZLoggerLogProcessorLoggerProvider(logProcessorFactory(options), options));
        });

        return builder;
    }
}

public class BatchingHttpLogProcessor : BatchingAsyncLogProcessor
{
    // https://github.com/Cysharp/ZLogger?tab=readme-ov-file#logprocessor

    private readonly string _uri;
    HttpClient httpClient;
    ArrayBufferWriter<byte> bufferWriter;
    IZLoggerFormatter formatter;

    public BatchingHttpLogProcessor(string uri, int batchSize, ZLoggerOptions options)
        : base(batchSize, options)
    {
        _uri = uri;
        httpClient = new HttpClient();
        bufferWriter = new ArrayBufferWriter<byte>();
        formatter = options.CreateFormatter();
    }

    protected override async ValueTask ProcessAsync(IReadOnlyList<INonReturnableZLoggerEntry> list)
    {
        foreach (var item in list)
        {
            item.FormatUtf8(bufferWriter, formatter);
            bufferWriter.Write("\n"u8);
        }

        var byteArrayContent = new ByteArrayContent(bufferWriter.WrittenSpan.ToArray());
        await httpClient.PostAsync(_uri, byteArrayContent).ConfigureAwait(false);

        bufferWriter.Clear();
    }

    protected override ValueTask DisposeAsyncCore()
    {
        httpClient.Dispose();
        return default;
    }
}

// CLEF MessageTemplate Formatter https://clef-json.org/

internal class CLEFMessageTemplateFormatter(AppLoggerOptions options) : IZLoggerFormatter
{
    static readonly JsonEncodedText Timestamp = JsonEncodedText.Encode("@t");
    static readonly JsonEncodedText Message = JsonEncodedText.Encode("@m");
    static readonly JsonEncodedText MessageTemplate = JsonEncodedText.Encode("@mt");
    static readonly JsonEncodedText Level = JsonEncodedText.Encode("@l");
    static readonly JsonEncodedText Exception = JsonEncodedText.Encode("@x");
    static readonly JsonEncodedText EventId = JsonEncodedText.Encode("@i");
    static readonly JsonEncodedText Renderings = JsonEncodedText.Encode("@r");

    static readonly JsonEncodedText LogLevelVerbose = JsonEncodedText.Encode("Verbose");
    static readonly JsonEncodedText LogLevelDebug = JsonEncodedText.Encode("Debug");
    static readonly JsonEncodedText LogLevelInformation = JsonEncodedText.Encode("Information");
    static readonly JsonEncodedText LogLevelWarning = JsonEncodedText.Encode("Warning");
    static readonly JsonEncodedText LogLevelError = JsonEncodedText.Encode("Error");
    static readonly JsonEncodedText LogLevelFatal = JsonEncodedText.Encode("Fatal");

    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public bool WithLineBreak => true;

    Utf8JsonWriter? jsonWriter;
    ArrayBufferWriter<byte> originalFormatWriter = new ArrayBufferWriter<byte>();

    public void FormatLogEntry(IBufferWriter<byte> writer, IZLoggerEntry entry)
    {
        // FormatLogEntry is guaranteed call in single-thread so reuse Utf8JsonWriter
        jsonWriter?.Reset(writer);
        jsonWriter ??= new Utf8JsonWriter(writer);

        jsonWriter.WriteStartObject();

        jsonWriter.WriteString(Timestamp, entry.LogInfo.Timestamp.Utc); // Utc or Local
        WriteLogLevel(jsonWriter, entry.LogInfo.LogLevel);

        // choose string Name or int Id
        if (entry.LogInfo.EventId.Name != null)
        {
            //  request: {"@t":"2025-06-19T23:10:19.022739+00:00","@l":"Warning","@i":null,"@mt":"App setup complete!"}
            // response: {"Error":"The `@i` event type value on line 1 is not in a string or numeric format."}
            jsonWriter.WriteString(EventId, entry.LogInfo.EventId.Name);
        }
        // jsonWriter.WriteNumber(EventId, entry.LogInfo.EventId.Id);

        // MessageTemplate
        originalFormatWriter.ResetWrittenCount();
        entry.WriteOriginalFormat(originalFormatWriter);
        jsonWriter.WriteString(MessageTemplate, originalFormatWriter.WrittenSpan);

        if (entry.LogInfo.Exception != null)
        {
            jsonWriter.WriteString(Exception, entry.LogInfo.Exception.ToString());
        }

        if (options.CaptureThreadInfo)
        {
            var k = "ThreadId";
            var v = entry.LogInfo.ThreadInfo.ThreadId.ToString();
            jsonWriter.WriteString(JsonEncodedText.Encode(k), JsonEncodedText.Encode(v));
        }

        if (options.StaticLogProps?.Length > 0)
        {
            var properties = options.StaticLogProps;
            for (var i = 0; i < properties.Length; i++)
            {
                var kv = properties[i];
                if (kv.Value is { } v)
                    jsonWriter.WriteString(JsonEncodedText.Encode(kv.Key), JsonEncodedText.Encode(v));
                else
                    jsonWriter.WriteNull(JsonEncodedText.Encode(kv.Key));
            }
        }

        var scopeState = entry.LogInfo.ScopeState;
        if (scopeState != null && !scopeState.IsEmpty)
        {
            var properties = scopeState.Properties;
            for (var i = 0; i < properties.Length; i++)
            {
                var kv = properties[i];
                switch (kv.Value)
                {
                    case { } v:
                        v = v.ToString() ?? string.Empty;
                        jsonWriter.WriteString(JsonEncodedText.Encode(kv.Key), JsonEncodedText.Encode((string)v));
                        break;
                    default:
                        jsonWriter.WriteNull(JsonEncodedText.Encode(kv.Key));
                        break;
                }
            }
        }

        // Parameters
        entry.WriteJsonParameterKeyValues(jsonWriter, JsonSerializerOptions);

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();
    }

    static void WriteLogLevel(Utf8JsonWriter writer, LogLevel logLevel)
    {
        // Mapping SeriLog(CLEF author)'s LogLevel
        switch (logLevel)
        {
            case LogLevel.Trace:
                writer.WriteString(Level, LogLevelVerbose);
                break;
            case LogLevel.Debug:
                writer.WriteString(Level, LogLevelDebug);
                break;
            case LogLevel.Information:
                // Ignore LogLevel.Information(it is default for compact format)
                break;
            case LogLevel.Warning:
                writer.WriteString(Level, LogLevelWarning);
                break;
            case LogLevel.Error:
                writer.WriteString(Level, LogLevelError);
                break;
            case LogLevel.Critical:
                writer.WriteString(Level, LogLevelFatal);
                break;
            default:
                break;
        }
    }
}
