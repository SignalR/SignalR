namespace Microsoft.AspNet.SignalR
{
    public class HubConfiguration : ConnectionConfiguration
    {
        /// <summary>
        /// Determines whether JavaScript proxies for the server-side hubs should be auto generated at {Path}/hubs.
        /// Defaults to true.
        /// </summary>
        public bool EnableJavaScriptProxies { get; set; }

        public HubConfiguration()
        {
            EnableJavaScriptProxies = true;
        }
    }
}
