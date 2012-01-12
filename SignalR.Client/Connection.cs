using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
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

        public event Action<string> Received;
        public event Action<Exception> Error;
        public event Action Closed;

        public Connection(string url)
        {
            if (!url.EndsWith("/"))
            {
                url += "/";
            }

            Url = url;
            Groups = Enumerable.Empty<string>();
            Items = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public ICredentials Credentials { get; set; }

        public IEnumerable<string> Groups { get; internal set; }

        public Func<string> Sending { get; set; }

        public string Url { get; private set; }

        public bool IsActive { get; private set; }

        public long? MessageId { get; set; }

        public string ConnectionId { get; set; }

        public IDictionary<string, object> Items { get; private set; }

        public Task Start()
        {
            return Start(Transport.ServerSentEvents);
        }

        public virtual Task Start(IClientTransport transport)
        {
            if (IsActive)
            {
                return TaskAsyncHelper.Empty;
            }

            IsActive = true;

            _transport = transport;

            string data = null;

            if (Sending != null)
            {
                data = Sending();
            }

            string negotiateUrl = Url + "negotiate";

            return HttpHelper.PostAsync(negotiateUrl, PrepareRequest).Then(response =>
            {
                string raw = response.ReadAsString();

                if (raw == null)
                {
                    throw new InvalidOperationException("Server negotiation failed.");
                }

                var negotiationResponse = JsonConvert.DeserializeObject<NegotiationResponse>(raw);

                VerifyProtocolVersion(negotiationResponse.ProtocolVersion);

                ConnectionId = negotiationResponse.ConnectionId;

                return _transport.Start(this, data).Then(() => _initialized = true);
            })
            .FastUnwrap();
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
                    Closed();
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
                Received(message);
            }
        }

        internal void OnError(Exception error)
        {
            if (Error != null)
            {
                Error(error);
            }
        }

        internal void PrepareRequest(HttpWebRequest request)
        {
#if SILVERLIGHT
            // Useragent is not possible to set with Silverlight, not on the UserAgent property of the request nor in the Headers key/value in the request
#else
#if WINDOWS_PHONE
            request.UserAgent = CreateUserAgentString("SignalR.Client.WP7");
#elif __ANDROID__
            request.UserAgent = CreateUserAgentString("SignalR.Client.MonoAndroid");
#elif __MONOTOUCH__
            request.UserAgent = CreateUserAgentString("SignalR.Client.MonoTouch");
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
    }
}
