﻿using System;
using System.Buffers;

namespace Websocket.Client
{
    internal abstract class RequestMessage { }

    internal class RequestTextMessage : RequestMessage
    {
        public string Text { get; }

        public RequestTextMessage(string text)
        {
            Text = text;
        }
    }

    internal class RequestBinaryMessage : RequestMessage
    {
        public byte[] Data { get; }

        public RequestBinaryMessage(byte[] data)
        {
            Data = data;
        }
    }

    internal class RequestBinarySegmentMessage : RequestMessage
    {
        public ArraySegment<byte> Data { get; }

        public RequestBinarySegmentMessage(ArraySegment<byte> data)
        {
            Data = data;
        }
    }

    internal class RequestBinarySequenceMessage : RequestMessage
    {
        public ReadOnlySequence<byte> Data { get; }

        public RequestBinarySequenceMessage(ReadOnlySequence<byte> data)
        {
            Data = data;
        }
    }
}
