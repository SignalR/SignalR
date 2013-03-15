using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Messaging
{
    internal class StoreLink
    {
        private const int MaxBufferSize = 1000;

        private readonly IScaleoutMapping[] _data;
        private int _offset;

        public StoreLink()
            : this(MaxBufferSize)
        {

        }

        public StoreLink(int size)
        {
            _data = new IScaleoutMapping[size];
        }

        public StoreLink Next { get; set; }

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
                IScaleoutMapping mapping = null;
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

                return null;
            }
        }

        public bool Add(IScaleoutMapping mapping)
        {
            _data[_offset++] = mapping;

            if (_offset >= _data.Length)
            {
                return false;
            }

            return true;
        }

        public bool HasValue(ulong id)
        {
            return id >= MinValue && id <= MaxValue;
        }

        public IScaleoutMapping FindMapping(ulong id)
        {
            int low = 0;
            int high = _offset;

            // First see if the numbers are contiguous
            if (MinValue.HasValue)
            {
                var index = (int)(id - MinValue.Value);
                IScaleoutMapping mapping = _data[index];

                if (mapping != null && mapping.Id == id)
                {
                    return mapping;
                }
            }

            // Binary search as a fallback
            while (low <= high)
            {
                int mid = (low + high) / 2;

                IScaleoutMapping mapping = _data[mid];

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
                    return mapping;
                }
            }

            return null;
        }
    }
}
