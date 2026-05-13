using System;
using System.Buffers;
using System.Threading.Channels;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Jobs;

namespace Websocket.Client.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
[ShortRunJob]
public class RequestMessageBenchmarks
{
    private readonly string _text = "echo_fast:benchmark";
    private Channel<BeforeRequestMessage> _beforeChannel = null!;
    private ChannelWriter<BeforeRequestMessage> _beforeWriter = null!;
    private ChannelReader<BeforeRequestMessage> _beforeReader = null!;
    private Channel<CurrentRequestMessage> _currentChannel = null!;
    private ChannelWriter<CurrentRequestMessage> _currentWriter = null!;
    private ChannelReader<CurrentRequestMessage> _currentReader = null!;

    [GlobalSetup]
    public void Setup()
    {
        var options = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        };

        _beforeChannel = Channel.CreateUnbounded<BeforeRequestMessage>(options);
        _beforeWriter = _beforeChannel.Writer;
        _beforeReader = _beforeChannel.Reader;
        _currentChannel = Channel.CreateUnbounded<CurrentRequestMessage>(options);
        _currentWriter = _currentChannel.Writer;
        _currentReader = _currentChannel.Reader;
    }

    [Benchmark(Baseline = true)]
    public int Before_ClassRequestMessage()
    {
        _beforeWriter.TryWrite(new BeforeTextRequestMessage(_text));
        _beforeReader.TryRead(out var message);

        return ((BeforeTextRequestMessage)message!).Text.Length;
    }

    [Benchmark]
    public int Current_StructRequestMessage()
    {
        _currentWriter.TryWrite(CurrentRequestMessage.TextMessage(_text));
        _currentReader.TryRead(out var message);

        return message.Text!.Length;
    }

    private abstract class BeforeRequestMessage
    {
    }

    private sealed class BeforeTextRequestMessage : BeforeRequestMessage
    {
        public BeforeTextRequestMessage(string text)
        {
            Text = text;
        }

        public string Text { get; }
    }

    private enum CurrentRequestMessageType
    {
        Text = 1
    }

    private readonly struct CurrentRequestMessage
    {
        private CurrentRequestMessage(CurrentRequestMessageType type, string? text = null)
        {
            Type = type;
            Text = text;
            BinarySegment = default;
            BinarySequence = default;
        }

        public CurrentRequestMessageType Type { get; }

        public string? Text { get; }

        public ArraySegment<byte> BinarySegment { get; }

        public ReadOnlySequence<byte> BinarySequence { get; }

        public static CurrentRequestMessage TextMessage(string text)
        {
            return new CurrentRequestMessage(CurrentRequestMessageType.Text, text: text);
        }
    }
}
