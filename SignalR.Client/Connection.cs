using System;
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

        public event Action<string> Received;
        public event Action<Exception> Error;
        public event Action Closed;

        private readonly IClientTransport _transport = new LongPollingTransport();

        public Connection(string url)
        {
            if (!url.EndsWith("/"))
            {
                url += "/";
            }

            Url = url;
            Groups = Enumerable.Empty<string>();
        }

        public ICredentials Credentials { get; set; }

        public IEnumerable<string> Groups { get; internal set; }

        public Func<string> Sending { get; set; }

        public string Url { get; private set; }

        public bool IsActive { get; private set; }

        public long? MessageId { get; set; }

        public string ClientId { get; set; }

        public virtual Task Start()
        {
            if (IsActive)
            {
                return TaskAsyncHelper.Empty;
            }

            IsActive = true;

            string data = null;

            if (Sending != null)
            {
                data = Sending();
            }

            string negotiateUrl = Url + "negotiate";

            return HttpHelper.PostAsync(negotiateUrl, PrepareRequest).Success(task =>
            {
                string raw = task.Result.ReadAsString();

                if (raw == null)
                {
                    throw new InvalidOperationException("Failed to negotiate");
                }

                var negotiationResponse = JsonConvert.DeserializeObject<NegotiationResponse>(raw);

                ClientId = negotiationResponse.ClientId;

                _transport.Start(this, data);
            });
        }

        public virtual void Stop()
        {
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
            }
        }

        public Task Send(string data)
        {
            return Send<object>(data);
        }

        public Task<T> Send<T>(string data)
        {
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
    }
}
