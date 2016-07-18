// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.IO;
using System.Globalization;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    internal class MemoryPoolTextWriter : TextWriter
    {
        private readonly IMemoryPool _memory;

        private char[] _textArray;
        private int _textBegin;
        private int _textEnd;
        // ReSharper disable InconsistentNaming
        private const int _textLength = 128;
        // ReSharper restore InconsistentNaming

        private byte[] _dataArray;
        private int _dataEnd;

        private readonly Encoder _encoder;

        public ArraySegment<byte> Buffer
        {
            get
            {
                return new ArraySegment<byte>(_dataArray, 0, _dataEnd);
            }
        }

        public MemoryPoolTextWriter(IMemoryPool memory)
            : base(CultureInfo.InvariantCulture)
        {
            _memory = memory;
            _textArray = _memory.AllocChar(_textLength);
            _dataArray = MemoryPool.EmptyArray;
            _encoder = Encoding.UTF8.GetEncoder();
        }

        public override Encoding Encoding
        {
            get
            {
                return Encoding.UTF8;
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (_textArray != null)
                    {
                        _memory.FreeChar(_textArray);
                        _textArray = null;
                    }
                    if (_dataArray != null)
                    {
                        _memory.FreeByte(_dataArray);
                        _dataArray = null;
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private void Encode(bool flush)
        {
            var bytesNeeded = _encoder.GetByteCount(
                _textArray,
                _textBegin,
                _textEnd - _textBegin,
                flush);

            Grow(bytesNeeded);

            var bytesUsed = _encoder.GetBytes(
                _textArray,
                _textBegin,
                _textEnd - _textBegin,
                _dataArray,
                _dataEnd,
                flush);

            _textBegin = _textEnd = 0;
            _dataEnd += bytesUsed;
        }

        protected void Grow(int minimumAvailable)
        {
            if (_dataArray.Length - _dataEnd >= minimumAvailable)
            {
                return;
            }

            var newLength = _dataArray.Length + Math.Max(_dataArray.Length, minimumAvailable);
            var newArray = _memory.AllocByte(newLength);
            Array.Copy(_dataArray, 0, newArray, 0, _dataEnd);
            _memory.FreeByte(_dataArray);
            _dataArray = newArray;
        }

        public override void Write(char value)
        {
            if (_textLength == _textEnd)
            {
                Encode(false);
                if (_textLength == _textEnd)
                {
                    throw new InvalidOperationException("Unexplainable failure to encode text");
                }
            }

            _textArray[_textEnd++] = value;
        }

        public override void Write(char[] value, int index, int length)
        {
            // this override exists as an optimization to avoid too many calls to Write(char)
            var sourceIndex = index;
            var sourceLength = index + length;
            while (sourceIndex < sourceLength)
            {
                if (_textLength == _textEnd)
                {
                    Encode(false);
                }

                var count = sourceLength - sourceIndex;
                if (count > _textLength - _textEnd)
                {
                    count = _textLength - _textEnd;
                }

                Array.Copy(value, sourceIndex, _textArray, _textEnd, count);
                sourceIndex += count;
                _textEnd += count;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public override void Write(string value)
        {
            var sourceIndex = 0;
            var sourceLength = value.Length;
            while (sourceIndex < sourceLength)
            {
                if (_textLength == _textEnd)
                {
                    Encode(false);
                }

                var count = sourceLength - sourceIndex;
                if (count > _textLength - _textEnd)
                {
                    count = _textLength - _textEnd;
                }

                value.CopyTo(sourceIndex, _textArray, _textEnd, count);
                sourceIndex += count;
                _textEnd += count;
            }
        }

        public override void Flush()
        {
            while (_textBegin != _textEnd)
            {
                Encode(true);
            }
        }

        public void Write(ArraySegment<byte> data)
        {
            Flush();

            Grow(data.Count);

            System.Buffer.BlockCopy(data.Array, data.Offset, _dataArray, _dataEnd, data.Count);
            _dataEnd += data.Count;
        }
    }
}
