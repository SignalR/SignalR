using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class TestRequest : IRequest
    {
        public Uri Url { get; set; }

        public string LocalPath { get; set; }

        public IDictionary<string, Cookie> Cookies { get; } = new Dictionary<string, Cookie>();

        public IPrincipal User { get; set; }

        public IDictionary<string, object> Environment { get; } = new Dictionary<string, object>();

        public NameValueCollection QueryString { get; } = new NameValueCollection();

        public NameValueCollection Headers { get; } = new NameValueCollection();

        public NameValueCollection Form {get; } = new NameValueCollection();

        INameValueCollection IRequest.QueryString => new NameValueCollectionWrapper(QueryString);

        INameValueCollection IRequest.Headers => new NameValueCollectionWrapper(Headers);

        public Task<INameValueCollection> ReadForm()
        {
            return Task.FromResult<INameValueCollection>(new NameValueCollectionWrapper(Form));
        }
    }
}
