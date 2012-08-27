using System.Collections.Generic;
using System.IO;
using Gate;

namespace SignalR.Server
{
    class ServerResponse : IResponse
    {
        private readonly Response _res;
        private readonly dynamic _clientConnected;

        public ServerResponse(Response res)
        {
            _res = res;
            _clientConnected = _res.Environment.Get<dynamic>("server.ClientConnected");
        }

        public bool IsClientConnected
        {
            get { return _clientConnected.IsConnected; }
        }

        public string ContentType
        {
            get { return _res.ContentType; }
            set { _res.ContentType = value; }
        }

        public IDictionary<string, string[]> Headers
        {
            get { return _res.Headers; }
        }

        public Stream OutputStream
        {
            get { return _res.OutputStream; }
        }
    }
}
