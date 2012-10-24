using System.Collections.Specialized;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public sealed class HubActivationResult
    {
        public IHub Hub { get; private set; }
        public NameValueCollection Items { get; private set; }

        public HubActivationResult(IHub hub)
        {
            Hub = hub;
            Items = new NameValueCollection();
        }
    }
}