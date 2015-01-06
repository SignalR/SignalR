// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNet.SignalR.Client.Transports.ServerSentEvents
{
    public class ChunkBuffer
    {
        private int _offset;
        private readonly List<char> _buffer;

        public ChunkBuffer()
        {
            _buffer = new List<char>();
        }

        public bool HasChunks
        {
            get
            {
                return _offset < _buffer.Count;
            }
        }

        public void Add(byte[] buffer, int length)
        {
            _buffer.AddRange(Encoding.UTF8.GetChars(buffer, 0, length));
        }

        public void Add(ArraySegment<byte> buffer)
        {
			_buffer.AddRange(Encoding.UTF8.GetChars(buffer.Array, buffer.Offset, buffer.Count));
        }

        public string ReadLine()
        {
            // Lock while reading so that we can make safe assuptions about the buffer indicies
            for (int i = _offset; i < _buffer.Count; i++, _offset++)
            {
                if (_buffer[i] == '\n') // List seems to have a faster getter than StringBuilder
                {
	                var chars = new char[_offset];
					_buffer.CopyTo(0, chars, 0, _offset);
                    _buffer.RemoveRange(0, _offset + 1);
					_offset = 0;
					
                    return new string(chars).Trim(); // we could be smarter on that trim and do it by hand before the CopyTo; I think it's mostly a NOP
                }
            }

            return null;
        }
    }
}
