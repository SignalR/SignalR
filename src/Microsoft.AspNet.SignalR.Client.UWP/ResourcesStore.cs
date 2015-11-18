using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Microsoft.AspNet.SignalR.Client
{
    internal static class ResourcesStore
    {
        private static ResourceManager resourceMan;

        internal static ResourceManager ResourceManager
        {
            get
            {
                if (ReferenceEquals(resourceMan, null))
                {
                    var assembly = typeof(Resources).GetTypeInfo().Assembly;
                    var temp = new ResourceManager("Microsoft.AspNet.SignalR.Client.Store.Resources", assembly);
                    resourceMan = temp;
                }

                return resourceMan;
            }
        }

        public static string GetResourceString(string resourceName)
        {
            return ResourceManager.GetString(resourceName, null);
        }
    }
}
