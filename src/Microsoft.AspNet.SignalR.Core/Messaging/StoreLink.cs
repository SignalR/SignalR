using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Messaging
{
    internal class StoreLink
    {
        private const int MaxBufferSize = 1000;

        private readonly ScaleoutMapping[] _data;
        private int _offset;

        private readonly object _lockObj = new object();

        public StoreLink()
            : this(MaxBufferSize)
        {

        }

        public StoreLink Next;

        public StoreLink(int size)
        {
            _data = new ScaleoutMapping[size];
        }

        public ArraySegment<ScaleoutMapping> GetSnapshot()
        {
            lock (_lockObj)
            {
                return new ArraySegment<ScaleoutMapping>(_data, 0, _offset);
            }
        }

        public ArraySegment<ScaleoutMapping> GetSnapshot(ulong id)
        {
            lock (_lockObj)
            {
                int index = FindMapping(id);

                if (index == -1)
                {
                    return new ArraySegment<ScaleoutMapping>();
                }

                return new ArraySegment<ScaleoutMapping>(_data, index, _offset - index);
            }
        }

        public ulong? MinValue
        {
            get
            {
                var mapping = _data[0];
                if (mapping != null)
                {
                    return mapping.Id;
                }

                return null;
            }
        }

        public ulong? MaxValue
        {
            get
            {
                ScaleoutMapping mapping = null;

                lock (_lockObj)
                {
                    if (_offset == 0)
                    {
                        mapping = _data[_offset];
                    }
                    else
                    {
                        mapping = _data[_offset - 1];
                    }

                    if (mapping != null)
                    {
                        return mapping.Id;
                    }
                }

                return null;
            }
        }

        public bool Add(ScaleoutMapping mapping)
        {
            lock (_lockObj)
            {
                _data[_offset++] = mapping;

                if (_offset >= _data.Length)
                {
                    return false;
                }

                return true;
            }
        }

        public bool HasValue(ulong id)
        {
            return id >= MinValue && id <= MaxValue;
        }

        private int FindMapping(ulong id)
        {
            int low = 0;
            int high = _offset;

            // First see if the numbers are contiguous
            if (MinValue.HasValue)
            {
                var index = (int)(id - MinValue.Value);
                ScaleoutMapping mapping = _data[index];

                if (mapping != null && mapping.Id == id)
                {
                    return index;
                }
            }

            // Binary search as a fallback
            while (low <= high)
            {
                int mid = (low + high) / 2;

                ScaleoutMapping mapping = _data[mid];

                if (id < mapping.Id)
                {
                    high = mid - 1;
                }
                else if (id > mapping.Id)
                {
                    low = mid + 1;
                }
                else if (id == mapping.Id)
                {
                    return mid;
                }
            }

            return -1;
        }
    }
}
