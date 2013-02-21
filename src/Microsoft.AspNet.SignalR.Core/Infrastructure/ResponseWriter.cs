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
    internal unsafe class ResponseWriter : TextWriter, IBinaryWriter
    {
        // Max chars to buffer before writing to the response
        private const int MaxChars = 1024;

        // Max bytes we allow allocating before we start chunking
        private const int MaxBytes = 2048;

        private readonly Encoding _encoding;
        private readonly Encoder _encoder;

        private readonly Action<ArraySegment<byte>, object> _write;
        private readonly object _writeState;

        private static readonly byte[] _colonBytes = new byte[] { 58 };
        private static readonly byte[] _doubleQuoteBytes = new byte[] { 34 };
        private static readonly byte[] _singleQuoteBytes = new byte[] { 39 };
        private static readonly byte[] _newLineBytes = new byte[] { 10 };
        private static readonly byte[] _commaBytes = new byte[] { 44 };

        private readonly bool _reuseBuffers;

        public ResponseWriter(IResponse response) :
            this((data, state) => ((IResponse)state).Write(data), response, reuseBuffers: true)
        {

        }

        public ResponseWriter(IWebSocket socket) :
            this((data, state) => ((IWebSocket)state).SendChunk(data), socket, reuseBuffers: false)
        {

        }

        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.IO.TextWriter.#ctor", Justification = "It won't be used")]
        public ResponseWriter(Action<ArraySegment<byte>, object> write, object state, bool reuseBuffers)
        {
            _write = write;
            _writeState = state;
            _encoding = new UTF8Encoding();
            _encoder = _encoding.GetEncoder();
            _reuseBuffers = reuseBuffers;
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
                    char* charBuffer = &value;
                    Write(charBuffer, 1);
                    break;
            }
        }

        private void WriteToResponse(string value)
        {
            var byteCount = Encoding.GetByteCount(value);

            if (byteCount >= MaxBytes)
            {
                var writer = new ChunkedWriter(_write, _writeState, MaxChars, Encoding, _reuseBuffers);
                writer.Write(value);
            }
            else
            {
                fixed (char* charBuffer = value)
                {
                    Write(charBuffer, value.Length, byteCount);
                }
            }
        }

        public void Write(ArraySegment<byte> data)
        {
            _write(data, _writeState);
        }

        private void Write(char* charBuffer, int charCount, int? byteCount = null)
        {
            byteCount = byteCount ?? _encoder.GetByteCount(charBuffer, charCount, flush: true);

            var buffer = new byte[Math.Max(1, byteCount.Value)];

            fixed (byte* byteBuffer = buffer)
            {
                int count = _encoder.GetBytes(charBuffer, charCount, byteBuffer, byteCount.Value, flush: false);
                Write(new ArraySegment<byte>(buffer, 0, count));
            }
        }

        private class ChunkedWriter
        {
            private int _charPos;
            private int _charLen;

            private readonly Encoder _encoder;
            private readonly char[] _charBuffer;
            private readonly byte[] _byteBuffer;
            private readonly Action<ArraySegment<byte>, object> _write;
            private readonly object _writeState;

            public ChunkedWriter(Action<ArraySegment<byte>, object> write, object state, int chunkSize, Encoding encoding, bool reuseBuffers)
            {
                _charLen = chunkSize;
                _charBuffer = new char[chunkSize];
                _write = write;
                _writeState = state;
                _encoder = encoding.GetEncoder();

                if (reuseBuffers)
                {
                    _byteBuffer = new byte[encoding.GetMaxByteCount(chunkSize)];
                }
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
                // If it's safe to reuse the buffer then do so
                if (_byteBuffer != null)
                {
                    Flush(_byteBuffer);
                }
                else
                {
                    // Allocate a byte array of the right size for this char buffer
                    int byteCount = _encoder.GetByteCount(_charBuffer, 0, _charPos, flush: false);
                    var byteBuffer = new byte[byteCount];
                    Flush(byteBuffer);
                }
            }

            private void Flush(byte[] byteBuffer)
            {
                int count = _encoder.GetBytes(_charBuffer, 0, _charPos, byteBuffer, 0, flush: true);

                _charPos = 0;

                if (count > 0)
                {
                    _write(new ArraySegment<byte>(byteBuffer, 0, count), _writeState);
                }
            }
        }
    }
}
