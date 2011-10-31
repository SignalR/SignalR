using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SignalR.Client.Hubs
{
    public class HubConnection : Connection
    {
        private readonly Dictionary<string, HubProxy> _hubs = new Dictionary<string, HubProxy>();

        public HubConnection(string url)
            : base(GetUrl(url))
        {
        }

        public override Task Start()
        {
            Sending += OnSending;
            Received += OnReceived;
            return base.Start();
        }

        public override void Stop()
        {
            Sending -= OnSending;
            Received -= OnReceived;
            base.Stop();
        }

        public IHubProxy CreateProxy(string hubName)
        {
            HubProxy hubProxy;
            if (!_hubs.TryGetValue(hubName, out hubProxy))
            {
                hubProxy = new HubProxy(this, hubName);
                _hubs[hubName] = hubProxy;
            }
            return hubProxy;
        }

        private string OnSending()
        {
            var data = _hubs.Select(p => new HubRegistrationData
            {
                Name = p.Key,
                Methods = p.Value.GetSubscriptions()
            });

            return JsonConvert.SerializeObject(data);
        }

        private void OnReceived(string message)
        {
            var invocation = JsonConvert.DeserializeObject<HubInvocation>(message);
            HubProxy hubProxy;
            if (_hubs.TryGetValue(invocation.Hub, out hubProxy))
            {
                if (invocation.State != null)
                {
                    foreach (var state in invocation.State)
                    {
                        hubProxy[state.Key] = state.Value;
                    }
                }

                hubProxy.InvokeEvent(invocation.Method, invocation.Args);
            }
        }

        private static string GetUrl(string url)
        {
            if (!url.EndsWith("/"))
            {
                url += "/";
            }
            return url + "signalr";
        }
    }
}
