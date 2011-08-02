using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SignalR.Client.Hubs {
    public class HubConnection : Connection {
        private readonly Dictionary<string, HubProxy> _hubs = new Dictionary<string, HubProxy>();

        public HubConnection(string url)
            : base(GetUrl(url)) {
        }

        public override Task Start() {
            Sending += OnSending;
            Received += OnReceived;
            return base.Start();
        }

        public override void Stop() {
            Sending -= OnSending;
            Received -= OnReceived;
            base.Stop();
        }

        public IHubProxy CreateProxy(string hub) {
            HubProxy hubProxy;
            if (!_hubs.TryGetValue(hub, out hubProxy)) {
                hubProxy = new HubProxy(this, hub);
                _hubs[hub] = hubProxy;
            }
            return hubProxy;
        }

        private string OnSending() {
            var data = _hubs.Select(p => new {
                Name = p.Key,
                Methods = p.Value.GetSubscriptions()
            });

            return JsonConvert.SerializeObject(data);
        }

        private void OnReceived(string message) {
            var invocationInfo = JsonConvert.DeserializeObject<HubInvocationInfo>(message);

            HubProxy hubProxy;
            if (_hubs.TryGetValue(invocationInfo.Hub, out hubProxy)) {
                hubProxy.OnReceived(invocationInfo);
            }
        }

        private static string GetUrl(string url) {
            if (!url.EndsWith("/")) {
                url += "/";
            }
            return url + "signalr";
        }
    }
}
