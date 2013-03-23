// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class ScaleoutMappingStore
    {
        private const int MaxMessages = 1000000;
        private ulong _maxKey = UInt64.MaxValue;

        private ScaleoutStore _store;

        public ScaleoutMappingStore()
        {
            _store = new ScaleoutStore(MaxMessages);
        }

        public void Add(ulong id, IList<LocalEventKeyInfo> localKeyInfo)
        {
            if (id < _maxKey)
            {
                // Do something here
            }

            _store.Add(new ScaleoutMapping(id, localKeyInfo));

            _maxKey = id;
        }

        public ulong MaxKey
        {
            get
            {
                return _maxKey;
            }
        }

        public IEnumerator<ScaleoutMapping> GetEnumerator(ulong id)
        {
            MessageStoreResult<ScaleoutMapping> result = _store.GetMessagesByMappingId(id);

            return new ScaleoutStoreEnumerator(_store, result);
        }

        private struct ScaleoutStoreEnumerator : IEnumerator<ScaleoutMapping>, IEnumerator
        {
            private readonly ScaleoutStore _store;
            private MessageStoreResult<ScaleoutMapping> _result;
            private int _offset;
            private int _length;
            private ulong _nextId;

            public ScaleoutStoreEnumerator(ScaleoutStore store, MessageStoreResult<ScaleoutMapping> result)
                : this()
            {
                _store = store;
                InitResult(result);
            }

            public ScaleoutMapping Current
            {
                get
                {
                    return _result.Messages.Array[_offset];
                }
            }

            public void Dispose()
            {

            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                _offset++;

                if (_offset < _length)
                {
                    return true;
                }

                if (!_result.HasMoreData)
                {
                    return false;
                }

                var result = _store.GetMessages(_nextId);
                InitResult(result);

                _offset++;

                return _offset < _length;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            private void InitResult(MessageStoreResult<ScaleoutMapping> result)
            {
                _result = result;
                _offset = _result.Messages.Offset - 1;
                _length = _result.Messages.Offset + _result.Messages.Count;
                _nextId = _result.FirstMessageId + (ulong)_result.Messages.Count;
            }

        }
    }
}
