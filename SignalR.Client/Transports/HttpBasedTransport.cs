using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SignalR.Client.Http;
#if NETFX_CORE
using Windows.UI.Xaml;
#endif

namespace SignalR.Client.Transports
{
    public abstract class HttpBasedTransport : IClientTransport
    {
        // The send query string
        private const string _sendQueryString = "?transport={0}&connectionId={1}{2}";

        // The transport name
        protected readonly string _transport;

        protected const string HttpRequestKey = "http.Request";

        protected readonly IHttpClient _httpClient;

        private bool _supportsKeepAlive;
        private bool _monitoringKeepAlive;
        private KeepAliveData _keepAliveData = new KeepAliveData();
#if NETFX_CORE
        private DispatcherTimer _keepAliveMonitor;
#else
        private Timer _keepAliveMonitor;
#endif
        // Used to ensure that only one thread can be in the check keep alive function at a time
        private Int32 _checkingKeepAlive = 0;

        public HttpBasedTransport(IHttpClient httpClient, string transport, bool supportsKeepAlive)
        {
            _httpClient = httpClient;
            _transport = transport;
            _supportsKeepAlive = supportsKeepAlive;
        }

        public Task<NegotiationResponse> Negotiate(IConnection connection)
        {
            return GetNegotiationResponse(_httpClient, connection);
        }

        internal static Task<NegotiationResponse> GetNegotiationResponse(IHttpClient httpClient, IConnection connection)
        {
#if SILVERLIGHT || WINDOWS_PHONE
            string negotiateUrl = connection.Url + "negotiate?" + GetNoCacheUrlParam();
#else
            string negotiateUrl = connection.Url + "negotiate";
#endif

            return httpClient.GetAsync(negotiateUrl, connection.PrepareRequest).Then(response =>
            {
                string raw = response.ReadAsString();

                if (raw == null)
                {
                    throw new InvalidOperationException("Server negotiation failed.");
                }

                return JsonConvert.DeserializeObject<NegotiationResponse>(raw);
            });
        }

        public Task Start(IConnection connection, string data)
        {
            var tcs = new TaskCompletionSource<object>();

            OnStart(connection, data, () => tcs.TrySetResult(null), exception => tcs.TrySetException(exception));

            return tcs.Task;
        }

        protected abstract void OnStart(IConnection connection, string data, Action initializeCallback, Action<Exception> errorCallback);

        public Task<T> Send<T>(IConnection connection, string data)
        {
            string url = connection.Url + "send";
            string customQueryString = GetCustomQueryString(connection);

            url += String.Format(_sendQueryString, _transport, connection.ConnectionId, customQueryString);

            var postData = new Dictionary<string, string> {
                { "data", data }
            };

            return _httpClient.PostAsync(url, connection.PrepareRequest, postData).Then(response =>
            {
                string raw = response.ReadAsString();

                if (String.IsNullOrEmpty(raw))
                {
                    return default(T);
                }

                return JsonConvert.DeserializeObject<T>(raw);
            });
        }

        protected string GetReceiveQueryString(IConnection connection, string data)
        {
            // ?transport={0}&connectionId={1}&messageId={2}&groups={3}&connectionData={4}{5}
            var qsBuilder = new StringBuilder();
            qsBuilder.Append("?transport=" + _transport)
                     .Append("&connectionId=" + Uri.EscapeDataString(connection.ConnectionId));

            if (connection.MessageId != null)
            {
                qsBuilder.Append("&messageId=" + Uri.EscapeDataString(connection.MessageId));
            }

            if (connection.Groups != null && connection.Groups.Any())
            {
                qsBuilder.Append("&groups=" + Uri.EscapeDataString(JsonConvert.SerializeObject(connection.Groups)));
            }

            if (data != null)
            {
                qsBuilder.Append("&connectionData=" + data);
            }

            string customQuery = GetCustomQueryString(connection);

            if (!String.IsNullOrEmpty(customQuery))
            {
                qsBuilder.Append("&")
                         .Append(customQuery);
            }

#if SILVERLIGHT || WINDOWS_PHONE
            qsBuilder.Append("&").Append(GetNoCacheUrlParam());
#endif
            return qsBuilder.ToString();
        }

        private static string GetNoCacheUrlParam()
        {
            return "noCache=" + Guid.NewGuid().ToString();
        }

        protected virtual Action<IRequest> PrepareRequest(IConnection connection)
        {
            return request =>
            {
                // Setup the user agent along with any other defaults
                connection.PrepareRequest(request);

                connection.Items[HttpRequestKey] = request;
            };
        }

        public void Stop(IConnection connection, bool notifyServer = true)
        {
            var httpRequest = connection.GetValue<IRequest>(HttpRequestKey);
            if (httpRequest != null)
            {
                try
                {
                    OnBeforeAbort(connection);

                    // Abort the server side connection
                    if (notifyServer)
                    {
                        AbortConnection(connection);
                    }

                    // Now abort the client connection
                    httpRequest.Abort();
                }
                catch (NotImplementedException)
                {
                    // If this isn't implemented then do nothing
                }
            }
        }

        private void AbortConnection(IConnection connection)
        {
            string url = connection.Url + "abort" + String.Format(_sendQueryString, _transport, connection.ConnectionId, null);

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

        protected static void ProcessResponse(IConnection connection, string response, out bool timedOut, out bool disconnected)
        {
            timedOut = false;
            disconnected = false;

            if (String.IsNullOrEmpty(response))
            {
                return;
            }

            try
            {
                var result = JValue.Parse(response);

                if (!result.HasValues)
                {
                    return;
                }

                timedOut = result.Value<bool>("TimedOut");
                disconnected = result.Value<bool>("Disconnect");

                if (disconnected)
                {
                    return;
                }

                var messages = result["Messages"] as JArray;
                if (messages != null)
                {
                    foreach (JToken message in messages)
                    {
                        try
                        {
                            connection.OnReceived(message);
                        }
                        catch (Exception ex)
                        {
#if NET35
                            Debug.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Failed to process message: {0}", ex));
#else
                            Debug.WriteLine("Failed to process message: {0}", ex);
#endif

                            connection.OnError(ex);
                        }
                    }

                    connection.MessageId = result["MessageId"].Value<string>();

                    var transportData = result["TransportData"] as JObject;

                    if (transportData != null)
                    {
                        var groups = (JArray)transportData["Groups"];
                        if (groups != null)
                        {
                            connection.Groups = groups.Select(token => token.Value<string>());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
#if NET35
                Debug.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Failed to response: {0}", ex));
#else
                Debug.WriteLine("Failed to response: {0}", ex);
#endif
                connection.OnError(ex);
            }
        }

        private static string GetCustomQueryString(IConnection connection)
        {
            return String.IsNullOrEmpty(connection.QueryString)
                            ? ""
                            : "&" + connection.QueryString;
        }

        #region Keep Alive Handling

        /// <summary>
        /// Start the timer for the keep alive monitoring
        /// </summary>
        /// <param name="connection">The current connection associated with the transport</param>
        public void MonitorKeepAlive(IConnection connection)
        {
            // Only monitor the keep alive if it's not already in the process of being monitored;
            if (!_monitoringKeepAlive)
            {
                _monitoringKeepAlive = true;

                // Initiate the keep alive timestamps
                UpdateKeepAlive();

#if NETFX_CORE
                _keepAliveMonitor = new DispatcherTimer();
                _keepAliveMonitor.Interval = _keepAliveData.KeepAliveCheckInterval;
                _keepAliveMonitor.Tick +=
                delegate
                {
                    CheckIfAlive(connection);
                };
                _keepAliveMonitor.Start();
#else
                _keepAliveMonitor = new Timer(CheckIfAlive, connection, _keepAliveData.KeepAliveCheckInterval, _keepAliveData.KeepAliveCheckInterval);
#endif
            }
        }

        /// <summary>
        /// Called by the keep alive monitor timer
        /// </summary>
        /// <param name="state">The current connection associated with the transport</param>
        private void CheckIfAlive(object state)
        {
            // Ensure that two threads cannot be in here simultaneously
            if (Interlocked.Exchange(ref _checkingKeepAlive, 1) == 0)
            {
                IConnection connection = state as IConnection;

                // Only check if we're connected
                if (connection.State == ConnectionState.Connected)
                {
                    TimeSpan timeElapsed = (DateTime.UtcNow - _keepAliveData.LastKeepAlive);

                    // Check if the keep alive has completely timed out
                    if (timeElapsed >= _keepAliveData.Timeout)
                    {
                        // Notify transport that the connection has been lost
                        LostConnection(connection);
                    }
                    else if (timeElapsed >= _keepAliveData.TimeoutWarning)
                    {
                        // This is to assure that the user only gets a single warning
                        if (!_keepAliveData.WarningTriggered)
                        {
                            connection.OnConnectionSlow();
                            _keepAliveData.WarningTriggered = true;
                        }
                    }
                    else
                    {
                        _keepAliveData.WarningTriggered = false;
                    }
                }

                _checkingKeepAlive = 0;
            }
        }

        /// <summary>
        /// Updates the last keep alive time stamp
        /// </summary>
        protected void UpdateKeepAlive()
        {
            _keepAliveData.LastKeepAlive = DateTime.UtcNow;
        }

        /// <summary>
        /// Initiates the Keep Alive data for the transport
        /// </summary>
        /// <param name="keepAlive">The server's keep alive configuration</param>
        public void RegisterKeepAlive(TimeSpan keepAlive)
        {
            if (SupportsKeepAlive())
            {
                // Setting the keep alive will calculate the monitoring thresholds
                _keepAliveData.KeepAlive = keepAlive;
            }
        }

        public bool SupportsKeepAlive()
        {
            return _supportsKeepAlive;
        }

        /// <summary>
        /// This is expected to be overriden by LongPollingTransport and ServerSentEvents
        /// </summary>
        /// <param name="connection"></param>
        public virtual void LostConnection(IConnection connection)
        {
        }

        /// <summary>
        /// Stops the monitoring of the keep alive.  Called when the connection is forcibly stopped.
        /// </summary>
        public void StopMonitoringKeepAlive()
        {
            _monitoringKeepAlive = false;

#if NETFX_CORE
            _keepAliveMonitor.Stop();
#else
            _keepAliveMonitor.Dispose();
#endif

            _keepAliveMonitor = null;
        }
        #endregion
    }
}
