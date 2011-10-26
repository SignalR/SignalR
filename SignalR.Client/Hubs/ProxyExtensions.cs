using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SignalR.Client.Hubs
{
    public static class ProxyExtensions
    {
        public static T GetValue<T>(this IHubProxy proxy, string name)
        {
            object value = proxy[name];
            if (value is JObject && typeof(T) != typeof(JObject))
            {
                return JsonConvert.DeserializeObject<T>(((JObject)value).ToString());
            }
            return (T)value;
        }
    }
}
