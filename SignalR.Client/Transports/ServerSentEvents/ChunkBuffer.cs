using System;
using System.Text;

namespace SignalR.Client.Transports.ServerSentEvents
{
    public class ChunkBuffer
    {
        private int _offset;
        private readonly StringBuilder _buffer;
        private readonly StringBuilder _lineBuilder;

        public ChunkBuffer()
        {
            _buffer = new StringBuilder();
            _lineBuilder = new StringBuilder();
        }

        public bool HasChunks
        {
            get
            {
                return _offset < _buffer.Length;
            }
        }

        public void Add(byte[] buffer, int length)
        {
            _buffer.Append(Encoding.UTF8.GetString(buffer, 0, length));
        }

        public string ReadLine()
        {
            // Lock while reading so that we can make safe assuptions about the buffer indicies
            for (int i = _offset; i < _buffer.Length; i++, _offset++)
            {
                if (_buffer[i] == '\n')
                {
                    _buffer.Remove(0, _offset + 1);

                    string line = _lineBuilder.ToString();
#if WINDOWS_PHONE
                    _lineBuilder.Length = 0;
#else
                    _lineBuilder.Clear();
#endif
                    _offset = 0;
                    return line;
                }

                _lineBuilder.Append(_buffer[i]);
            }

            return null;
        }
    }
}
