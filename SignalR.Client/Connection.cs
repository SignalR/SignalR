using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SignalR.Client.Transports;

namespace SignalR.Client
{
    public class Connection : IConnection
    {
        private static Version _assemblyVersion;

        private IClientTransport _transport;
        private bool _initialized;

        private readonly SynchronizationContext _syncContext;

        public event Action<string> Received;
        public event Action<Exception> Error;
        public event Action Closed;
        public event Action Reconnected;

        public Connection(string url)
            : this(url, (string)null)
        {

        }

        public Connection(string url, IDictionary<string, string> queryString)
            : this(url, CreateQueryString(queryString))
        {

        }

        public Connection(string url, string queryString)
        {
            if (url.Contains('?'))
            {
                throw new ArgumentException("Url cannot contain QueryString directly. Pass QueryString values in using available overload.", "url");
            }

            if (!url.EndsWith("/"))
            {
                url += "/";
            }

            Url = url;
            QueryString = queryString;
            Groups = Enumerable.Empty<string>();
            Items = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _syncContext = SynchronizationContext.Current;
        }

        public ICredentials Credentials { get; set; }

        public IEnumerable<string> Groups { get; internal set; }

        public Func<string> Sending { get; set; }

        public string Url { get; private set; }

        public bool IsActive { get; private set; }

        public long? MessageId { get; set; }

        public string ConnectionId { get; set; }

        public IDictionary<string, object> Items { get; private set; }

        public string QueryString { get; private set; }

        public Task Start()
        {
#if WINDOWS_PHONE || SILVERLIGHT
            return Start(new LongPollingTransport());
#else
            // Pick the best transport supported by the client
            return Start(new AutoTransport());
#endif
        }

        public virtual Task Start(IClientTransport transport)
        {
            if (IsActive)
            {
                return TaskAsyncHelper.Empty;
            }

            IsActive = true;

            _transport = transport;

            return Negotiate();
        }

        private Task Negotiate()
        {
            string negotiateUrl = Url + "negotiate";

            var negotiateTcs = new TaskCompletionSource<object>();

            HttpHelper.PostAsync(negotiateUrl, PrepareRequest).Then(response =>
            {
                string raw = response.ReadAsString();

                if (raw == null)
                {
                    throw new InvalidOperationException("Server negotiation failed.");
                }

                var negotiationResponse = JsonConvert.DeserializeObject<NegotiationResponse>(raw);

                VerifyProtocolVersion(negotiationResponse.ProtocolVersion);

                ConnectionId = negotiationResponse.ConnectionId;

                if (Sending != null)
                {
                    if (_syncContext != null)
                    {
                        var dataTcs = new TaskCompletionSource<string>();
                        _syncContext.Post(_ =>
                        {
                            // Raise the event on the sync context
                            dataTcs.SetResult(Sending());
                        },
                        null);

                        // Get the data and start the transport
                        dataTcs.Task.Then(data => StartTransport(data))
                                    .FastUnwrap()
                                    .ContinueWith(negotiateTcs);
                    }
                    else
                    {
                        var data = Sending();
                        StartTransport(data).ContinueWith(negotiateTcs);
                    }
                }
                else
                {
                    StartTransport(null).ContinueWith(negotiateTcs);
                }
            })
            .ContinueWithNotComplete(negotiateTcs);

            return negotiateTcs.Task;
        }

        private Task StartTransport(string data)
        {
            return _transport.Start(this, data)
                             .Then(() => _initialized = true);
        }

        private void VerifyProtocolVersion(string versionString)
        {
            Version version;
            if (String.IsNullOrEmpty(versionString) ||
                !TryParseVersion(versionString, out version) ||
                !(version.Major == 1 && version.Minor == 0))
            {
                throw new InvalidOperationException("Incompatible protocol version.");
            }
        }

        public virtual void Stop()
        {
            // Do nothing if the connection was never started
            if (!_initialized)
            {
                return;
            }

            try
            {
                _transport.Stop(this);

                if (Closed != null)
                {
                    if (_syncContext != null)
                    {
                        _syncContext.Post(_ => Closed(), null);
                    }
                    else
                    {
                        Closed();
                    }
                }
            }
            finally
            {
                IsActive = false;
                _initialized = false;
            }
        }

        public Task Send(string data)
        {
            return Send<object>(data);
        }

        public Task<T> Send<T>(string data)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Start must be called before data can be sent");
            }

            return _transport.Send<T>(this, data);
        }

        internal void OnReceived(string message)
        {
            if (Received != null)
            {
                if (_syncContext != null)
                {
                    _syncContext.Post(msg => Received((string)msg), message);
                }
                else
                {
                    Received(message);
                }
            }
        }

        internal void OnError(Exception error)
        {
            if (Error != null)
            {
                if (_syncContext != null)
                {
                    _syncContext.Post(err => Error((Exception)err), error);
                }
                else
                {
                    Error(error);
                }
            }
        }

        internal void OnReconnected()
        {
            if (Reconnected != null)
            {
                if (_syncContext != null)
                {
                    _syncContext.Post(_ => Reconnected(), null);
                }
                else
                {
                    Reconnected();
                }
            }
        }

        internal void PrepareRequest(HttpWebRequest request)
        {
#if WINDOWS_PHONE
            // http://msdn.microsoft.com/en-us/library/ff637320(VS.95).aspx
            request.UserAgent = CreateUserAgentString("SignalR.Client.WP7");
#else
#if SILVERLIGHT
            // Useragent is not possible to set with Silverlight, not on the UserAgent property of the request nor in the Headers key/value in the request
#else
            request.UserAgent = CreateUserAgentString("SignalR.Client");
#endif
#endif
            if (Credentials != null)
            {
                request.Credentials = Credentials;
            }
        }

        private static string CreateUserAgentString(string client)
        {
            if (_assemblyVersion == null)
            {
                _assemblyVersion = new AssemblyName(typeof(Connection).Assembly.FullName).Version;
            }

            return String.Format(CultureInfo.InvariantCulture, "{0}/{1} ({2})", client, _assemblyVersion, Environment.OSVersion);
        }

        private static bool TryParseVersion(string versionString, out Version version)
        {
#if WINDOWS_PHONE
            try
            {
                version = new Version(versionString);
                return true;
            }
            catch
            {
                version = null;
                return false;
            }
#else
            return Version.TryParse(versionString, out version);
#endif
        }

        private static string CreateQueryString(IDictionary<string, string> queryString)
        {
            return String.Join("&", queryString.Select(kvp => kvp.Key + "=" + kvp.Value).ToArray());
        }
    }
}
