using System.Buffers;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace Websocket.Client.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
[RankColumn]
public class TextSendEncodingBenchmarks
{
    private static readonly Encoding Encoding = Encoding.UTF8;
    private string _message = null!;

    [Params(32, 1024, 8192)]
    public int MessageLength { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _message = new string('x', MessageLength);
    }

    [Benchmark(Baseline = true, Description = "Before: Encoding.GetBytes")]
    public int Before_GetBytesArray()
    {
        var payload = Encoding.GetBytes(_message);
        return payload.Length;
    }

    [Benchmark(Description = "Current: ArrayPool encode")]
    public int Current_ArrayPoolEncode()
    {
        var byteCount = Encoding.GetByteCount(_message);
        var buffer = ArrayPool<byte>.Shared.Rent(byteCount);

        try
        {
            return Encoding.GetBytes(_message, 0, _message.Length, buffer, 0);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
