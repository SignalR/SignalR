using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SignalR.Server.Infrastructure;

namespace SignalR.Server
{
    public partial class ServerResponse
    {
        private readonly IDictionary<string, object> _env;

        public ServerResponse(IDictionary<string, object> env)
        {
            _env = env;
            _callCancelled = Get<CancellationToken>(OwinConstants.CallCancelled);
        }

        private T Get<T>(string key)
        {
            object value;
            return _env.TryGetValue(key, out value) ? (T)value : default(T);
        }

        public IDictionary<string, string[]> ResponseHeaders
        {
            get { return Get<IDictionary<string, string[]>>(OwinConstants.ResponseHeaders); }
        }

        public Stream ResponseBody
        {
            get { return Get<Stream>(OwinConstants.ResponseBody); }
        }

        public Action DisableResponseBuffering
        {
            get { return Get<Action>(OwinConstants.DisableResponseBuffering) ?? (() => { }); }
        }

    }
}