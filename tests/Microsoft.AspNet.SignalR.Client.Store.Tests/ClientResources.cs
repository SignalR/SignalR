
using System.Globalization;
using System.Reflection;
using System.Resources;
using Microsoft.AspNet.SignalR.Client.Transports;

namespace Microsoft.AspNet.SignalR.Client.Store.Tests
{
    internal static class ResourceUtil
    {
        private static readonly ResourceManager ResourceManager;

        static ResourceUtil()
        {
            var assembly = typeof(ClientTransportBase).GetTypeInfo().Assembly;
            ResourceManager = new ResourceManager("Microsoft.AspNet.SignalR.Client.Resources", assembly);
        }

        public static string GetResource(string resourceName)
        {
            return ResourceManager.GetString(resourceName, CultureInfo.CurrentCulture);
        }
    }
}
