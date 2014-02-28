// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNet.SignalR.Infrastructure
{    
    // Similar to MemoryStream, but tries to allocate as few objects as possible on the LOH

    internal sealed class ByteBuffer
    {

        private int _currentLength;
        private readonly int? _maxLength;
        private readonly List<byte[]> _segments = new List<byte[]>();

        public ByteBuffer(int? maxLength)
        {
            _maxLength = maxLength;
        }

        public void Append(byte[] segment)
        {
            checked { _currentLength += segment.Length; }
            if (_maxLength.HasValue && _currentLength > _maxLength)
            {
                throw new InvalidOperationException("Buffer length exceeded");
            }

            _segments.Add(segment);
        }

        // returns the segments as a single byte array
        public byte[] GetByteArray()
        {
            byte[] newArray = new byte[_currentLength];
            int lastOffset = 0;

            for (int i = 0; i < _segments.Count; i++)
            {
                byte[] thisSegment = _segments[i];
                Buffer.BlockCopy(thisSegment, 0, newArray, lastOffset, thisSegment.Length);
                lastOffset += thisSegment.Length;
            }

            return newArray;
        }

        // treats the segments as UTF8-encoded information and returns the resulting string
        public string GetString()
        {
            StringBuilder builder = new StringBuilder();
            Decoder decoder = Encoding.UTF8.GetDecoder();

            for (int i = 0; i < _segments.Count; i++)
            {
                bool flush = (i == _segments.Count - 1);
                byte[] thisSegment = _segments[i];
                int charsRequired = decoder.GetCharCount(thisSegment, 0, thisSegment.Length, flush);
                char[] thisSegmentAsChars = new char[charsRequired];
                int numCharsConverted = decoder.GetChars(thisSegment, 0, thisSegment.Length, thisSegmentAsChars, 0, flush);
                builder.Append(thisSegmentAsChars, 0, numCharsConverted);
            }

            return builder.ToString();
        }

    }
}
