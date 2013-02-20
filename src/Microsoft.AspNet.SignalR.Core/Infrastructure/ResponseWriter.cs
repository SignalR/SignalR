using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Microsoft.AspNet.SignalR.Hosting;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    /// <summary>
    /// TextWriter implementation over IResponse optimized for writing in small chunks
    /// we don't need to write to a long lived buffer. This saves massive amounts of memory
    /// as the number of connections grows.
    /// </summary>
    internal class ResponseWriter : TextWriter, IBinaryWriter
    {
        // Max chars to buffer before writing to the response
        private const int MaxChars = 1024;

        // Max bytes we allow allocating before we start chunking
        private const int MaxBytes = 2048;

        private readonly Encoding _encoding = new UTF8Encoding();

        private readonly Action<ArraySegment<byte>, object> _write;
        private readonly object _writeState;

        private static readonly byte[] _colonBytes = new byte[] { 58 };
        private static readonly byte[] _doubleQuoteBytes = new byte[] { 34 };
        private static readonly byte[] _singleQuoteBytes = new byte[] { 39 };
        private static readonly byte[] _newLineBytes = new byte[] { 10 };
        private static readonly byte[] _commaBytes = new byte[] { 44 };

        public ResponseWriter(IResponse response) :
            this((data, state) => ((IResponse)state).Write(data), response)
        {

        }

        public ResponseWriter(IWebSocket socket) :
            this((data, state) => ((IWebSocket)state).SendChunk(data), socket)
        {

        }

        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.IO.TextWriter.#ctor", Justification = "It won't be used")]
        public ResponseWriter(Action<ArraySegment<byte>, object> write, object state)
        {
            _write = write;
            _writeState = state;
        }

        public override Encoding Encoding
        {
            get { return _encoding; }
        }

        public override void Write(string value)
        {
            WriteToResponse(value);
        }

        public override void WriteLine(string value)
        {
            WriteToResponse(value);
        }

        public override void Write(char value)
        {
            switch (value)
            {
                case ':':
                    Write(new ArraySegment<byte>(_colonBytes));
                    break;
                case '"':
                    Write(new ArraySegment<byte>(_doubleQuoteBytes));
                    break;
                case '\'':
                    Write(new ArraySegment<byte>(_singleQuoteBytes));
                    break;
                case '\n':
                    Write(new ArraySegment<byte>(_newLineBytes));
                    break;
                case ',':
                    Write(new ArraySegment<byte>(_commaBytes));
                    break;
                default:
                    Write(new ArraySegment<byte>(Encoding.GetBytes(new[] { value })));
                    break;
            }
        }

        private void WriteToResponse(string value)
        {
            var byteCount = Encoding.GetByteCount(value);

            if (byteCount >= MaxBytes)
            {
                var writer = new ChunkedWriter(_write, _writeState, MaxChars, Encoding);
                writer.Write(value);
            }
            else
            {
                Write(new ArraySegment<byte>(Encoding.GetBytes(value)));
            }
        }

        public void Write(ArraySegment<byte> data)
        {
            _write(data, _writeState);
        }

        private class ChunkedWriter
        {
            private int _charPos;
            private int _charLen;

            private readonly Encoding _encoding;
            private readonly char[] _charBuffer;
            private readonly byte[] _byteBuffer;
            private readonly Action<ArraySegment<byte>, object> _write;
            private readonly object _writeState;

            public ChunkedWriter(Action<ArraySegment<byte>, object> write, object state, int chunkSize, Encoding encoding)
            {
                _encoding = encoding;
                _charLen = chunkSize;
                _charBuffer = new char[chunkSize];
                _byteBuffer = new byte[_encoding.GetMaxByteCount(chunkSize)];
                _write = write;
                _writeState = state;
            }

            public void Write(string value)
            {
                int length = value.Length;
                int sourceIndex = 0;

                while (length > 0)
                {
                    if (_charPos == _charLen)
                    {
                        Flush();
                    }

                    int count = _charLen - _charPos;
                    if (count > length)
                    {
                        count = length;
                    }

                    value.CopyTo(sourceIndex, _charBuffer, _charPos, count);
                    _charPos += count;
                    sourceIndex += count;
                    length -= count;
                }

                Flush();
            }

            private void Flush()
            {
                int count = _encoding.GetBytes(_charBuffer, 0, _charPos, _byteBuffer, 0);
                _charPos = 0;

                if (count > 0)
                {
                    _write(new ArraySegment<byte>(_byteBuffer, 0, count), _writeState);
                }
            }
        }
    }
}
