using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Jobs;

namespace Websocket.Client.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
[ShortRunJob]
public class SendSequenceFramingBenchmarks
{
    private ReadOnlySequence<byte> _payload;
    private byte[][] _segments = [];
    private int _observedBytes;
    private int _observedFrames;
    private bool _observedEndOfMessage;

    [Params(2, 4, 8)]
    public int SegmentCount { get; set; }

    [Params(16, 256)]
    public int SegmentSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _segments = new byte[SegmentCount][];

        for (var i = 0; i < _segments.Length; i++)
        {
            _segments[i] = new byte[SegmentSize];
            _segments[i][0] = (byte)i;
        }

        _payload = CreateSequence(_segments);
    }

    [Benchmark(Baseline = true)]
    public int Previous_FinalEmptyFrame()
    {
        ResetObservedState();

        foreach (var memory in _payload)
        {
            SendFrame(memory, endOfMessage: false);
        }

        SendFrame(ReadOnlyMemory<byte>.Empty, endOfMessage: true);
        return _observedFrames + _observedBytes + (_observedEndOfMessage ? 1 : 0);
    }

    [Benchmark]
    public int Current_FinalSegmentCompletesMessage()
    {
        ResetObservedState();

        var segments = _payload.GetEnumerator();
        if (!segments.MoveNext())
        {
            SendFrame(ReadOnlyMemory<byte>.Empty, endOfMessage: true);
            return _observedFrames + _observedBytes + (_observedEndOfMessage ? 1 : 0);
        }

        var current = segments.Current;
        while (segments.MoveNext())
        {
            SendFrame(current, endOfMessage: false);
            current = segments.Current;
        }

        SendFrame(current, endOfMessage: true);
        return _observedFrames + _observedBytes + (_observedEndOfMessage ? 1 : 0);
    }

    private void ResetObservedState()
    {
        _observedBytes = 0;
        _observedFrames = 0;
        _observedEndOfMessage = false;
    }

    private void SendFrame(ReadOnlyMemory<byte> payload, bool endOfMessage)
    {
        _observedBytes += payload.Length;
        _observedFrames++;
        _observedEndOfMessage = endOfMessage;
    }

    private static ReadOnlySequence<byte> CreateSequence(byte[][] segments)
    {
        var first = new BufferSegment(segments[0]);
        var last = first;

        for (var i = 1; i < segments.Length; i++)
        {
            last = last.Append(segments[i]);
        }

        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    private sealed class BufferSegment : ReadOnlySequenceSegment<byte>
    {
        public BufferSegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public BufferSegment Append(ReadOnlyMemory<byte> memory)
        {
            var segment = new BufferSegment(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };

            Next = segment;
            return segment;
        }
    }
}
