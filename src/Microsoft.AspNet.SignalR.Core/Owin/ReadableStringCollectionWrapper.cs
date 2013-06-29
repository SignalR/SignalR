using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
