using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SignalR.Client.Transports;

namespace SignalR.Client {
    public class Connection {
        public event Action<string> Received;
        public event Func<string> Sending;
        public event Action Closed;

        private readonly IClientTransport _transport = new LongPollingTransport();

        public Connection(string url) {
            if (!url.StartsWith("/")) {
                url += "/";
            }

            Url = url;
        }

        public string Url { get; private set; }

        internal long? MessageId { get; set; }

        internal string ClientId { get; set; }

        public bool IsActive { get; private set; }

        public virtual Task Start() {
            if (IsActive) {
                return TaskAsyncHelper.Empty;
            }

            IsActive = true;

            string data = String.Empty;
            if (Sending != null) {
                data = Sending();
            }
            
            string negotiateUrl = Url + "negotiate";

            return HttpHelper.PostAsync(negotiateUrl).Success(task => {
                string raw = task.Result.ReadAsString();

                var negotiationResponse = JsonConvert.DeserializeObject<NegotiationResponse>(raw);

                ClientId = negotiationResponse.ClientId;

                _transport.Start(this, data);
            });
        }

        public virtual void Stop() {
            try {                
                _transport.Stop(this);

                if (Closed != null) {
                    Closed();
                }
            }
            finally {
                IsActive = false;
            }
        }

        public Task Send(string data) {
            return Send<object>(data);
        }

        public Task<T> Send<T>(string data) {
            return _transport.Send<T>(this, data);
        }        

        internal void RaiseOnReceived(string message) {
            if (Received != null) {
                Received(message);
            }
        }
    }
}
