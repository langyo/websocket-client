using System;
using System.Buffers;

namespace Websocket.Client
{
    internal enum RequestMessageType
    {
        None = 0,
        Text = 1,
        Binary = 2,
        BinarySegment = 3,
        BinarySequence = 4
    }

    internal readonly struct RequestMessage
    {
        private RequestMessage(
            RequestMessageType type,
            string? text = null,
            byte[]? binary = null,
            ArraySegment<byte> binarySegment = default,
            ReadOnlySequence<byte> binarySequence = default)
        {
            Type = type;
            Text = text;
            Binary = binary;
            BinarySegment = binarySegment;
            BinarySequence = binarySequence;
        }

        public RequestMessageType Type { get; }

        public string? Text { get; }

        public byte[]? Binary { get; }

        public ArraySegment<byte> BinarySegment { get; }

        public ReadOnlySequence<byte> BinarySequence { get; }

        public static RequestMessage TextMessage(string text)
        {
            return new RequestMessage(RequestMessageType.Text, text: text);
        }

        public static RequestMessage BinaryMessage(byte[] binary)
        {
            return new RequestMessage(RequestMessageType.Binary, binary: binary);
        }

        public static RequestMessage BinarySegmentMessage(ArraySegment<byte> binary)
        {
            return new RequestMessage(RequestMessageType.BinarySegment, binarySegment: binary);
        }

        public static RequestMessage BinarySequenceMessage(ReadOnlySequence<byte> binary)
        {
            return new RequestMessage(RequestMessageType.BinarySequence, binarySequence: binary);
        }
    }
}
