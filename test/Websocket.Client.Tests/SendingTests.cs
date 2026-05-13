using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Websocket.Client.Tests.TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Websocket.Client.Tests
{
    public class SendingTests
    {
        private readonly TestContext<SimpleStartup> _context;
        private readonly ITestOutputHelper _output;

        public SendingTests(ITestOutputHelper output)
        {
            _output = output;
            _context = new TestContext<SimpleStartup>(_output);
        }

        [Fact]
        public async Task SendMessageBeforeStart_ShouldWorkAfterStart()
        {
            using var client = _context.CreateClient();
            string received = null;
            var receivedCount = 0;
            var receivedEvent = new ManualResetEvent(false);

            client.Send("ping");
            client.Send("ping");
            client.Send("ping");
            client.Send("ping");

            client
                .MessageReceived
                .Where(x => x.Text.ToLower().Contains("pong"))
                .Subscribe(msg =>
                {
                    receivedCount++;
                    received = msg.Text;

                    if (receivedCount >= 7)
                        receivedEvent.Set();
                });

            await client.Start();

            client.Send("ping");
            client.Send("ping");
            client.Send("ping");

            receivedEvent.WaitOne(TimeSpan.FromSeconds(30));

            Assert.NotNull(received);
        }

        [Fact]
        public async Task SendBinaryMessage_ShouldWork()
        {
            using var client = _context.CreateClient();
            byte[] received = null;
            var receivedEvent = new ManualResetEvent(false);

            client.MessageReceived
                .Where(x => x.MessageType == WebSocketMessageType.Binary)
                .Subscribe(msg =>
                {
                    received = msg.Binary;
                    receivedEvent.Set();
                });

            await client.Start();
            client.Send([10, 14, 15, 16]);

            receivedEvent.WaitOne(TimeSpan.FromSeconds(30));

            Assert.NotNull(received);
            Assert.Equal(4, received.Length);
            Assert.Equal(14, received[1]);
        }

        [Fact]
        public async Task SendArraySegmentMessage_ShouldWork()
        {
            using var client = _context.CreateClient();
            byte[] received = null;
            var receivedEvent = new ManualResetEvent(false);

            client.MessageReceived
                .Where(x => x.MessageType == WebSocketMessageType.Binary)
                .Subscribe(msg =>
                {
                    received = msg.Binary;
                    receivedEvent.Set();
                });

            await client.Start();
            client.Send(new ArraySegment<byte>([10, 14, 15, 16]));

            receivedEvent.WaitOne(TimeSpan.FromSeconds(30));

            Assert.NotNull(received);
            Assert.Equal(4, received.Length);
            Assert.Equal(14, received[1]);
        }

        [Fact]
        public async Task SendSequenceMessage_ShouldWork()
        {
            using var client = _context.CreateClient();
            byte[] received = null;
            var receivedEvent = new ManualResetEvent(false);

            client.MessageReceived
                .Where(x => x.MessageType == WebSocketMessageType.Binary)
                .Subscribe(msg =>
                {
                    received = msg.Binary;
                    receivedEvent.Set();
                });

            await client.Start();
            client.Send(new ReadOnlySequence<byte>([10, 14, 15, 16]));

            receivedEvent.WaitOne(TimeSpan.FromSeconds(30));

            Assert.NotNull(received);
            Assert.Equal(4, received.Length);
            Assert.Equal(14, received[1]);
        }

        [Fact]
        [Trait("Cat", "Sending")]
        public async Task SendSequenceMessage_MultipleSegments_ShouldCompleteFinalSegmentWithoutEmptyFrame()
        {
            var socket = new RecordingWebSocket(expectedSendFrames: 2);
            using var client = new WebsocketClient(
                new Uri("wss://example.com"),
                null,
                (_, _) => Task.FromResult<WebSocket>(socket))
            {
                ErrorReconnectTimeout = null,
                IsReconnectionEnabled = false,
                ReconnectTimeout = null
            };

            await client.Start();

            var sequence = CreateSequence([10, 14], [15, 16]);

            Assert.True(client.Send(sequence));
            await socket.WaitForSendsAsync();

            var frames = socket.SentFrames;
            Assert.Equal(2, frames.Count);
            Assert.Equal(WebSocketMessageType.Binary, frames[0].MessageType);
            Assert.False(frames[0].EndOfMessage);
            Assert.Equal([10, 14], frames[0].Payload);
            Assert.Equal(WebSocketMessageType.Binary, frames[1].MessageType);
            Assert.True(frames[1].EndOfMessage);
            Assert.Equal([15, 16], frames[1].Payload);
        }

        [Fact]
        [Trait("Cat", "Sending")]
        public async Task SendAsText_AllBinaryPayloadOverloads_ShouldSendTextFrames()
        {
            using var client = _context.CreateClient();
            var receivedCount = 0;
            var receivedEvent = new ManualResetEvent(false);

            client.MessageReceived
                .Where(x => x.Text == "pong")
                .Subscribe(_ =>
                {
                    if (Interlocked.Increment(ref receivedCount) >= 3)
                        receivedEvent.Set();
                });

            await client.Start();

            var payload = Encoding.UTF8.GetBytes("ping");
            Assert.True(client.SendAsText(payload));
            Assert.True(client.SendAsText(new ArraySegment<byte>(payload)));
            Assert.True(client.SendAsText(CreateSequence([112, 105], [110, 103])));

            receivedEvent.WaitOne(TimeSpan.FromSeconds(30));

            Assert.Equal(3, receivedCount);
        }

        [Fact]
        public async Task SendMessageAfterDispose_ShouldDoNothing()
        {
            using var client = _context.CreateClient();
            string received = null;
            var receivedCount = 0;
            var receivedEvent = new ManualResetEvent(false);

            client
                .MessageReceived
                .Where(x => x.Text.ToLower().Contains("pong"))
                .Subscribe(msg =>
                {
                    receivedCount++;
                    received = msg.Text;

                    if (receivedCount >= 3)
                        receivedEvent.Set();
                });

            await client.Start();

            client.Send("ping");
            client.Send("ping");
            client.Send("ping");

            await Task.Delay(100);
            receivedEvent.WaitOne(TimeSpan.FromSeconds(Debugger.IsAttached ? 30 : 3));

            client.Dispose();

            await Task.Delay(200);

            client.Send("ping");
            await client.SendInstant("ping");

            await Task.Delay(100);

            Assert.NotNull(received);
            Assert.Equal(3, receivedCount);
        }

        private static ReadOnlySequence<byte> CreateSequence(params byte[][] segments)
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

        private sealed class RecordingWebSocket : WebSocket
        {
            private readonly int _expectedSendFrames;
            private readonly List<SentFrame> _sentFrames = new List<SentFrame>();
            private readonly TaskCompletionSource<bool> _sendFramesReached = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            private WebSocketState _state = WebSocketState.Open;

            public RecordingWebSocket(int expectedSendFrames)
            {
                _expectedSendFrames = expectedSendFrames;
            }

            public IReadOnlyList<SentFrame> SentFrames
            {
                get
                {
                    lock (_sentFrames)
                    {
                        return _sentFrames.ToArray();
                    }
                }
            }

            public override WebSocketCloseStatus? CloseStatus { get; }

            public override string CloseStatusDescription { get; }

            public override WebSocketState State => _state;

            public override string SubProtocol => null;

            public Task WaitForSendsAsync()
            {
                return _sendFramesReached.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }

            public override void Abort()
            {
                _state = WebSocketState.Aborted;
            }

            public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
            {
                _state = WebSocketState.Closed;
                return Task.CompletedTask;
            }

            public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
            {
                _state = WebSocketState.CloseSent;
                return Task.CompletedTask;
            }

            public override void Dispose()
            {
                _state = WebSocketState.Closed;
            }

            public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true);
            }

            public override async ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                return new ValueWebSocketReceiveResult(0, WebSocketMessageType.Close, true);
            }

            public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
            {
                RecordSend(new ReadOnlyMemory<byte>(buffer.Array, buffer.Offset, buffer.Count), messageType, endOfMessage);
                return Task.CompletedTask;
            }

            public override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
            {
                RecordSend(buffer, messageType, endOfMessage);
                return ValueTask.CompletedTask;
            }

            private void RecordSend(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage)
            {
                lock (_sentFrames)
                {
                    _sentFrames.Add(new SentFrame(buffer.ToArray(), messageType, endOfMessage));

                    if (_sentFrames.Count >= _expectedSendFrames)
                    {
                        _sendFramesReached.TrySetResult(true);
                    }
                }
            }
        }

        private sealed class SentFrame
        {
            public SentFrame(byte[] payload, WebSocketMessageType messageType, bool endOfMessage)
            {
                Payload = payload;
                MessageType = messageType;
                EndOfMessage = endOfMessage;
            }

            public byte[] Payload { get; }

            public WebSocketMessageType MessageType { get; }

            public bool EndOfMessage { get; }
        }
    }
}
