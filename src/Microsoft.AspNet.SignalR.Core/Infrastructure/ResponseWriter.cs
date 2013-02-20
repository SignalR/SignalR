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

        private readonly IResponse _response;

        private static readonly byte[] _colonBytes = new byte[] { 58 };
        private static readonly byte[] _doubleQuoteBytes = new byte[] { 34 };
        private static readonly byte[] _singleQuoteBytes = new byte[] { 39 };
        private static readonly byte[] _newLineBytes = new byte[] { 10 };
        private static readonly byte[] _commaBytes = new byte[] { 44 };
        
        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.IO.TextWriter.#ctor", Justification = "It won't be used")]
        public ResponseWriter(IResponse response)
        {
            _response = response;
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
                    _response.Write(new ArraySegment<byte>(_colonBytes));
                    break;
                case '"':
                    _response.Write(new ArraySegment<byte>(_doubleQuoteBytes));
                    break;
                case '\'':
                    _response.Write(new ArraySegment<byte>(_singleQuoteBytes));
                    break;
                case '\n':
                    _response.Write(new ArraySegment<byte>(_newLineBytes));
                    break;
                case ',':
                    _response.Write(new ArraySegment<byte>(_commaBytes));
                    break;
                default:
                    _response.Write(new ArraySegment<byte>(Encoding.GetBytes(new[] { value })));
                    break;
            }
        }

        private void WriteToResponse(string value)
        {
            var byteCount = Encoding.GetByteCount(value);

            if (byteCount >= MaxBytes)
            {
                var writer = new ChunkedWriter(_response, MaxChars, Encoding);
                writer.Write(value);
            }
            else
            {
                _response.Write(new ArraySegment<byte>(Encoding.GetBytes(value)));
            }
        }

        public void Write(ArraySegment<byte> data)
        {
            _response.Write(data);
        }

        private class ChunkedWriter
        {
            private int _charPos;
            private int _charLen;

            private readonly Encoding _encoding;
            private readonly char[] _charBuffer;
            private readonly byte[] _byteBuffer;
            private readonly IResponse _response;

            public ChunkedWriter(IResponse response, int chunkSize, Encoding encoding)
            {
                _encoding = encoding;
                _charLen = chunkSize;
                _charBuffer = new char[chunkSize];
                _byteBuffer = new byte[_encoding.GetMaxByteCount(chunkSize)];
                _response = response;
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
                    _response.Write(new ArraySegment<byte>(_byteBuffer, 0, count));
                }
            }
        }        
    }
}
