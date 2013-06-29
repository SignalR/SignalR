using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.AspNet.SignalR.Hosting;

namespace Microsoft.AspNet.SignalR.Tests.Common.Infrastructure
{
    public class NameValueCollectionWrapper : INameValueCollection
    {
        private readonly NameValueCollection _collection;

        public NameValueCollectionWrapper()
            : this(new NameValueCollection())
        {
        }

        public NameValueCollectionWrapper(NameValueCollection collection)
        {
            _collection = collection;
        }

        public string this[string key]
        {
            get { return _collection[key]; }
        }

        public IEnumerable<string> GetValues(string key)
        {
            return _collection.GetValues(key);
        }

        public string Get(string key)
        {
            return _collection.Get(key);
        }
    }
}
