using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.Client.Store.Tests.Fakes
{
    internal class FakeConnection : IConnection, IFake
    {
        private readonly FakeInvocationManager _invocationManager = new FakeInvocationManager();

        public Version Protocol { get; set; }

        public TimeSpan TransportConnectTimeout { get; set; }

        public TimeSpan TotalTransportConnectTimeout { get; set; }

        public TimeSpan ReconnectWindow { get; set; }

        public KeepAliveData KeepAliveData
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string MessageId { get; set; }

        public string GroupsToken { get; set; }

        public IDictionary<string, object> Items
        {
            get { throw new NotImplementedException(); }
        }

        public string ConnectionId
        {
            get { throw new NotImplementedException(); }
        }

        public string ConnectionToken { get; set; }

        public string Url { get; set; }

        public string QueryString { get; set; }

        public ConnectionState State { get; set; }

        public IClientTransport Transport
        {
            get { throw new NotImplementedException(); }
        }

        public DateTime LastMessageAt
        {
            get { throw new NotImplementedException(); }
        }

        public DateTime LastActiveAt { get; set; }

        public bool ChangeState(ConnectionState oldState, ConnectionState newState)
        {
            _invocationManager.AddInvocation("ChangeState", oldState, newState);

            return _invocationManager.GetReturnValue<bool>("ChangeState");            
        }

        public IDictionary<string, string> Headers
        {
            get { throw new NotImplementedException(); }
        }

        public ICredentials Credentials
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public CookieContainer CookieContainer
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public JsonSerializer JsonSerializer
        {
            get { throw new NotImplementedException(); }
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void Stop(Exception error)
        {
            _invocationManager.AddInvocation("Stop", error);
        }

        public void Disconnect()
        {
            _invocationManager.AddInvocation("Disconnect");
        }

        public Task Send(string data)
        {
            throw new NotImplementedException();
        }

        public void OnReceived(Newtonsoft.Json.Linq.JToken data)
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception ex)
        {
            _invocationManager.AddInvocation("OnError", ex);
        }

        public void OnReconnecting()
        {
            _invocationManager.AddInvocation("OnReconnecting");
        }

        public void OnReconnected()
        {
            _invocationManager.AddInvocation("OnReconnected");
        }

        public void OnConnectionSlow()
        {
            throw new NotImplementedException();
        }

        public void PrepareRequest(IRequest request)
        {
            _invocationManager.AddInvocation("PrepareRequest", request);
        }

        public void MarkLastMessage()
        {
            _invocationManager.AddInvocation("MarkLastMessage");
        }

        public void MarkActive()
        {
            throw new NotImplementedException();
        }

        public void Trace(TraceLevels level, string format, params object[] args)
        {
            _invocationManager.AddInvocation("Trace", level, format, args);
        }

        public void Verify(string methodName, List<object[]> expectedParameters)
        {
            _invocationManager.Verify(methodName, expectedParameters);
        }

        public IEnumerable<object[]> GetInvocations(string methodName)
        {
            return _invocationManager.GetInvocations(methodName);
        }

        public void Setup<T>(string methodName, Func<T> behavior)
        {
            _invocationManager.AddSetup(methodName, behavior);
        }
    }
}
