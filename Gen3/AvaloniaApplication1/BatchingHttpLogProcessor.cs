// https://github.com/Cysharp/ZLogger?tab=readme-ov-file#logprocessor

using System.Buffers;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ZLogger;

namespace AvaloniaApplication1;

public class BatchingHttpLogProcessor : BatchingAsyncLogProcessor
{
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
        // formatter = options.UseFormatter(() => new CLEFMessageTemplateFormatter()).CreateFormatter();
        // formatter = options.UseCompactLogEventFormat().CreateFormatter();
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
