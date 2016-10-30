﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class ScaleoutMappingStore
    {
        private const int MaxMessages = 1000000;

        private ScaleoutStore _store;

        public ScaleoutMappingStore()
        {
            _store = new ScaleoutStore(MaxMessages);
        }

        public void Add(ulong id, ScaleoutMessage message, IList<LocalEventKeyInfo> localKeyInfo, TraceSource trace)
        {
            if (MaxMapping != null && id <= MaxMapping.Id)
            {
                _store = new ScaleoutStore(MaxMessages);
                trace.TraceError("Duplicate message id {0} from backplane happened, it may because of backplane reset, create a new ScaleoutStore", id);
            }

            _store.Add(new ScaleoutMapping(id, message, localKeyInfo));
        }

        public ScaleoutMapping MaxMapping
        {
            get
            {
                return _store.MaxMapping;
            }
        }

        public IEnumerator<ScaleoutMapping> GetEnumerator(ulong id)
        {
            MessageStoreResult<ScaleoutMapping> result = _store.GetMessagesByMappingId(id);

            return new ScaleoutStoreEnumerator(_store, result);
        }

        private struct ScaleoutStoreEnumerator : IEnumerator<ScaleoutMapping>, IEnumerator
        {
            private readonly WeakReference _storeReference;
            private MessageStoreResult<ScaleoutMapping> _result;
            private int _offset;
            private int _length;
            private ulong _nextId;

            public ScaleoutStoreEnumerator(ScaleoutStore store, MessageStoreResult<ScaleoutMapping> result)
                : this()
            {
                _storeReference = new WeakReference(store);
                Initialize(result);
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

                // If the store falls out of scope
                var store = (ScaleoutStore)_storeReference.Target;

                if (store == null)
                {
                    return false;
                }

                // Get the next result
                MessageStoreResult<ScaleoutMapping> result = store.GetMessages(_nextId);
                Initialize(result);

                _offset++;

                return _offset < _length;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            private void Initialize(MessageStoreResult<ScaleoutMapping> result)
            {
                _result = result;
                _offset = _result.Messages.Offset - 1;
                _length = _result.Messages.Offset + _result.Messages.Count;
                _nextId = _result.FirstMessageId + (ulong)_result.Messages.Count;
            }
        }
    }
}
