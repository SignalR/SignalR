using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Messaging
{
    // Represents a message store that is backed by a ring buffer.
    public sealed class ScaleoutStore
    {
        private const uint _minFragmentCount = 4;
        private static readonly uint _maxFragmentSize = (IntPtr.Size == 4) ? (uint)16384 : (uint)8192; // guarantees that fragments never end up in the LOH

        private Fragment[] _fragments;
        private readonly uint _fragmentSize;

        private long _minMessageId;
        private long _nextFreeMessageId;

        // Creates a message store with the specified capacity. The actual capacity will be *at least* the
        // specified value. That is, GetMessages may return more data than 'capacity'.
        public ScaleoutStore(uint capacity)
        {
            // set a minimum capacity
            if (capacity < 32)
            {
                capacity = 32;
            }

            // Dynamically choose an appropriate number of fragments and the size of each fragment.
            // This is chosen to avoid allocations on the large object heap and to minimize contention
            // in the store. We allocate a small amount of additional space to act as an overflow
            // buffer; this increases throughput of the data structure.
            checked
            {
                uint fragmentCount = Math.Max(_minFragmentCount, capacity / _maxFragmentSize);
                _fragmentSize = Math.Min((capacity + fragmentCount - 1) / fragmentCount, _maxFragmentSize);
                _fragments = new Fragment[fragmentCount + 1]; // +1 for the overflow buffer
            }
        }

        // only for testing purposes
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Only for testing")]
        public ulong GetMessageCount()
        {
            return (ulong)Volatile.Read(ref _nextFreeMessageId);
        }

        public uint FragmentSize
        {
            get
            {
                return _fragmentSize;
            }
        }

        public int FragmentCount
        {
            get
            {
                return _fragments.Length;
            }
        }

        // Adds a message to the store. Returns the ID of the newly added message.
        public ulong Add(ScaleoutMapping mapping)
        {
            // keep looping in TryAddImpl until it succeeds
            ulong newMessageId;
            while (!TryAddImpl(mapping, out newMessageId)) ;

            // When TryAddImpl succeeds, record the fact that a message was just added to the
            // store. We increment the next free id rather than set it explicitly since
            // multiple threads might be trying to write simultaneously. There is a nifty
            // side effect to this: _nextFreeMessageId will *always* return the total number
            // of messages that *all* threads agree have ever been added to the store. (The
            // actual number may be higher, but this field will eventually catch up as threads
            // flush data.)
            Interlocked.Increment(ref _nextFreeMessageId);
            return newMessageId;
        }

        private void GetFragmentOffsets(ulong messageId, out ulong fragmentNum, out int idxIntoFragmentsArray, out int idxIntoFragment)
        {
            fragmentNum = messageId / _fragmentSize;

            // from the bucket number, we can figure out where in _fragments this data sits
            idxIntoFragmentsArray = (int)(fragmentNum % (uint)_fragments.Length);
            idxIntoFragment = (int)(messageId % _fragmentSize);
        }

        private int GetFragmentOffset(ulong messageId)
        {
            ulong fragmentNum = messageId / _fragmentSize;

            return (int)(fragmentNum % (uint)_fragments.Length);
        }

        private ulong GetMessageId(ulong fragmentNum, uint offset)
        {
            return fragmentNum * _fragmentSize + offset;
        }

        private bool TryAddImpl(ScaleoutMapping mapping, out ulong newMessageId)
        {
            ulong nextFreeMessageId = (ulong)Volatile.Read(ref _nextFreeMessageId);

            // locate the fragment containing the next free id, which is where we should write
            ulong fragmentNum;
            int idxIntoFragmentsArray, idxIntoFragment;
            GetFragmentOffsets(nextFreeMessageId, out fragmentNum, out idxIntoFragmentsArray, out idxIntoFragment);
            Fragment fragment = _fragments[idxIntoFragmentsArray];

            if (fragment == null || fragment.FragmentNum < fragmentNum)
            {
                // the fragment is outdated (or non-existent) and must be replaced
                bool overwrite = fragment != null && fragment.FragmentNum < fragmentNum;

                if (idxIntoFragment == 0)
                {
                    // this thread is responsible for creating the fragment
                    Fragment newFragment = new Fragment(fragmentNum, _fragmentSize);
                    newFragment.Data[0] = mapping;
                    Fragment existingFragment = Interlocked.CompareExchange(ref _fragments[idxIntoFragmentsArray], newFragment, fragment);
                    if (existingFragment == fragment)
                    {
                        newMessageId = GetMessageId(fragmentNum, offset: 0);
                        newFragment.MinId = newMessageId;
                        newFragment.Length = 1;
                        newFragment.MaxId = GetMessageId(fragmentNum, offset: _fragmentSize - 1);

                        // Move the minimum id when we overwrite
                        if (overwrite)
                        {
                            _minMessageId = (long)(existingFragment.MaxId + 1);
                        }

                        return true;
                    }
                }

                // another thread is responsible for updating the fragment, so fall to bottom of method
            }
            else if (fragment.FragmentNum == fragmentNum)
            {
                // the fragment is valid, and we can just try writing into it until we reach the end of the fragment
                ScaleoutMapping[] fragmentData = fragment.Data;
                for (int i = idxIntoFragment; i < fragmentData.Length; i++)
                {
                    ScaleoutMapping originalMapping = Interlocked.CompareExchange(ref fragmentData[i], mapping, null);
                    if (originalMapping == null)
                    {
                        newMessageId = GetMessageId(fragmentNum, offset: (uint)i);
                        fragment.Length++;
                        return true;
                    }
                }

                // another thread used the last open space in this fragment, so fall to bottom of method
            }

            // failure; caller will retry operation
            newMessageId = 0;
            return false;
        }

        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This does alot of work")]
        public ArraySegment<ScaleoutMapping> GetMinimum()
        {
            var minMessageId = (ulong)Volatile.Read(ref _minMessageId);

            while (true)
            {
                ulong fragmentNum;
                int idxIntoFragmentsArray, idxIntoFragment;
                GetFragmentOffsets(minMessageId, out fragmentNum, out idxIntoFragmentsArray, out idxIntoFragment);

                Fragment fragment = _fragments[idxIntoFragmentsArray];

                // If we have the right data then return it
                if (fragment.FragmentNum == fragmentNum)
                {
                    return new ArraySegment<ScaleoutMapping>(fragment.Data, 0, fragment.Length);
                }

                minMessageId = (ulong)Volatile.Read(ref _minMessageId);
            }
        }

        public bool TryBinarySearch(ulong mappingId, out ArraySegment<ScaleoutMapping> mapping)
        {
            mapping = new ArraySegment<ScaleoutMapping>();

            long low = _minMessageId;
            long high = _nextFreeMessageId;

            while (low <= high)
            {
                var mid = (ulong)((low + high) / 2);

                int midOffset = GetFragmentOffset(mid);

                Fragment fragment = _fragments[midOffset];

                if (mappingId < fragment.MinValue)
                {
                    high = (long)(fragment.MinId - 1);
                }
                else if (mappingId > fragment.MaxValue)
                {
                    low = (long)(fragment.MaxId + 1);
                }
                else if (fragment.HasValue(mappingId))
                {
                    int index = fragment.BinarySearch(mappingId);

                    // Return the mapping for this fragment
                    mapping = new ArraySegment<ScaleoutMapping>(fragment.Data, index, fragment.Length - index);

                    return true;
                }
            }

            return false;
        }

        private sealed class Fragment
        {
            public readonly ulong FragmentNum;
            public readonly ScaleoutMapping[] Data;
            public int Length;
            public ulong MinId;
            public ulong MaxId;

            public Fragment(ulong fragmentNum, uint fragmentSize)
            {
                FragmentNum = fragmentNum;
                Data = new ScaleoutMapping[fragmentSize];
            }

            public ulong? MinValue
            {
                get
                {
                    var mapping = Data[0];
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

                    if (Length == 0)
                    {
                        mapping = Data[Length];
                    }
                    else
                    {
                        mapping = Data[Length - 1];
                    }

                    if (mapping != null)
                    {
                        return mapping.Id;
                    }

                    return null;
                }
            }

            public bool HasValue(ulong id)
            {
                return id >= MinValue && id <= MaxValue;
            }

            public int BinarySearch(ulong id)
            {
                int low = 0;
                int high = Length;

                while (low <= high)
                {
                    int mid = (low + high) / 2;

                    ScaleoutMapping mapping = Data[mid];

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

}
