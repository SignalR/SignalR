// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.Owin;

namespace Microsoft.AspNet.SignalR.Owin
{
    internal class ReadableStringCollectionWrapper : INameValueCollection
    {
        private readonly IReadableStringCollection _readableStringCollection;

        public ReadableStringCollectionWrapper(IReadableStringCollection readableStringCollection)
        {
            _readableStringCollection = readableStringCollection;
        }

        public string this[string key]
        {
            get
            {
                return _readableStringCollection[key];
            }
        }

        public IEnumerable<string> GetValues(string key)
        {
            return _readableStringCollection.GetValues(key);
        }


        public string Get(string key)
        {
            return _readableStringCollection.Get(key);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return GetEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private IEnumerable<KeyValuePair<string, string>> GetEnumerable()
        {
            foreach (var pair in _readableStringCollection)
            {
                yield return new KeyValuePair<string, string>(pair.Key, _readableStringCollection.Get(pair.Key));
            }
        }

    }
}
