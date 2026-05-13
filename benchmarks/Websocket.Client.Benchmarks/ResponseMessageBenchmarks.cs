using System.Net.WebSockets;
using BenchmarkDotNet.Attributes;

namespace Websocket.Client.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
[RankColumn]
public class ResponseMessageBenchmarks
{
    private byte[] _payload = null!;
    private MemoryStream _stream = null!;
    private ResponseMessage _streamMessage = null!;
    private ResponseMessage _arrayMessage = null!;

    [Params(64, 4096, 32768)]
    public int MessageSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _payload = Enumerable.Repeat((byte)'x', MessageSize).ToArray();
        _stream = new MemoryStream(_payload, writable: false);
        _streamMessage = ResponseMessage.BinaryStreamMessage(_stream);
        _arrayMessage = ResponseMessage.BinaryMessage(_payload);
    }

    [Benchmark(Baseline = true, Description = "Before: Stream ToArray in ToString")]
    public string Before_StreamToArrayToString()
    {
        return $"Type binary, length: {_stream.ToArray().Length}";
    }

    [Benchmark(Description = "Current: Stream Length in ToString")]
    public string Current_StreamLengthToString()
    {
        return _streamMessage.ToString();
    }

    [Benchmark(Description = "Current: Array-backed ToString")]
    public string Current_ArrayBackedToString()
    {
        return _arrayMessage.ToString();
    }
}
