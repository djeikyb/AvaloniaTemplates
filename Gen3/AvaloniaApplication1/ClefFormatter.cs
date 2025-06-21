// https://raw.githubusercontent.com/Cysharp/ZLogger/refs/tags/2.5.10/sandbox/ConsoleApp/SampleCustomFormatter.cs

using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace AvaloniaApplication1;


// CLEF MessageTemplate Formatter https://clef-json.org/

internal class CLEFMessageTemplateFormatter : IZLoggerFormatter
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
