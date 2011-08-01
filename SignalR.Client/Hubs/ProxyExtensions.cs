using Newtonsoft.Json.Linq;
using SignalR.Client;
using Newtonsoft.Json;

namespace SignalR.Client.Hubs {
    public static class ProxyExtensions {
        public static T GetValue<T>(this IHubProxy proxy, string name) {
            object value = proxy[name];
            if (value is JObject && typeof(T) != typeof(JObject)) {
                return JsonConvert.DeserializeObject<T>(((JObject)value).ToString());
            }
            return (T)value;
        }
    }
}
