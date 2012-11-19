// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public abstract class HttpBasedTransport : IClientTransport
    {
        // The send query string
        private const string _sendQueryString = "?transport={0}&connectionId={1}{2}";

        // The transport name
        private readonly string _transport;

        protected const string HttpRequestKey = "http.Request";

        private readonly IHttpClient _httpClient;

        protected HttpBasedTransport(IHttpClient httpClient, string transport)
        {
            _httpClient = httpClient;
            _transport = transport;
        }

        protected IHttpClient HttpClient
        {
            get { return _httpClient; }
        }

        public Task<NegotiationResponse> Negotiate(IConnection connection)
        {
            return TransportHelper.GetNegotiationResponse(_httpClient, connection);
        }

        public Task Start(IConnection connection, string data)
        {
            var tcs = new TaskCompletionSource<object>();

            OnStart(connection, data, () => tcs.TrySetResult(null), exception => tcs.TrySetException(exception));

            return tcs.Task;
        }

        protected abstract void OnStart(IConnection connection, string data, Action initializeCallback, Action<Exception> errorCallback);

        public Task Send(IConnection connection, string data)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            string url = connection.Url + "send";
            string customQueryString = TransportHelper.GetCustomQueryString(connection);

            url += String.Format(CultureInfo.InvariantCulture, _sendQueryString, _transport, connection.ConnectionId, customQueryString);

            var postData = new Dictionary<string, string> {
                { "data", data }
            };

            return _httpClient.PostAsync(url, connection.PrepareRequest, postData)
                              .Then(response =>
                              {
                                  string raw = response.ReadAsString();

                                  if (!String.IsNullOrEmpty(raw))
                                  {
                                      connection.OnReceived(JObject.Parse(raw));
                                  }
                              })
                              .Catch(connection.OnError);
        }

        protected string GetReceiveQueryString(IConnection connection, string data)
        {
            return TransportHelper.GetReceiveQueryString(connection, data, _transport);
        }

        protected virtual Action<IRequest> PrepareRequest(IConnection connection)
        {
            return request =>
            {
                // Setup the user agent along with any other defaults
                connection.PrepareRequest(request);

                lock (connection.Items)
                {
                    connection.Items[HttpRequestKey] = request;
                }
            };
        }

        public void Stop(IConnection connection)
        {
            var httpRequest = connection.GetValue<IRequest>(HttpRequestKey);
            if (httpRequest != null)
            {
                try
                {
                    OnBeforeAbort(connection);

                    // Abort the server side connection
                    AbortConnection(connection);

                    // Now abort the client connection
                    httpRequest.Abort();
                }
                catch (NotImplementedException)
                {
                    // If this isn't implemented then do nothing
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We don't want Stop to throw. IHttpClient.PostAsync could throw anything.")]
        private void AbortConnection(IConnection connection)
        {
            string url = connection.Url + "abort" + String.Format(CultureInfo.InvariantCulture, _sendQueryString, _transport, connection.ConnectionId, null);

            try
            {
                // Attempt to perform a clean disconnect, but only wait 2 seconds
                _httpClient.PostAsync(url, connection.PrepareRequest).Wait(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                // Swallow any exceptions, but log them
                Debug.WriteLine("Clean disconnect failed. " + ex.Unwrap().Message);
            }
        }


        protected virtual void OnBeforeAbort(IConnection connection)
        {

        }
    }
}
