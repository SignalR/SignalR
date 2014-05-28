using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;

namespace Microsoft.AspNet.SignalR.Client.Store.Tests.Fakes
{
    internal class FakeTransportHelper : TransportHelper
    {
        private const string UnexpectedParameterValueMessage =
            "The value passed to the method '{0}' is different than the expected value.";

        private readonly NegotiationResponse _negotiationResponse;
        private readonly IHttpClient _expectedClient;
        private readonly IConnection _expectedConnection;
        private readonly string _expectedConnectionData;

        public FakeTransportHelper(IHttpClient expectedClient, IConnection expectedConnection, string expectedConnectionData, NegotiationResponse negotiationResponse)
        {
            _expectedClient = expectedClient;
            _expectedConnection = expectedConnection;
            _expectedConnectionData = expectedConnectionData;
            _negotiationResponse = negotiationResponse;
        }

        public override Task<NegotiationResponse> GetNegotiationResponse(IHttpClient httpClient, IConnection connection, string connectionData)
        {
            if (httpClient != _expectedClient)
            {
                throw new ArgumentException(string.Format(UnexpectedParameterValueMessage, "GetNegotiationResponse"), "httpClient");
            }

            if (connection != _expectedConnection)
            {
                throw new ArgumentException(string.Format(UnexpectedParameterValueMessage, "GetNegotiationResponse"), "connection");
            }

            if (connectionData != _expectedConnectionData)
            {
                throw new ArgumentException(string.Format(UnexpectedParameterValueMessage, "GetNegotiationResponse"), "connectionData");
            }

            return Task.FromResult(_negotiationResponse);
        }
 
    }
}
