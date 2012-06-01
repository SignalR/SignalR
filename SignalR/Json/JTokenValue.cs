using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SignalR
{
    /// <summary>
    /// An implementation of IJsonValue over JSON.NET
    /// </summary>
    internal class JTokenValue : IJsonValue
    {
        private readonly JToken _value;

        public JTokenValue(JToken value)
        {
            _value = value;
        }

        public object ConvertTo(Type type)
        {
            // A non generic implementation of ToObject<T> on JToken
            using (var jsonReader = new JTokenReader(_value))
            {
                var serializer = new JsonSerializer();
                return serializer.Deserialize(jsonReader, type);
            }
        }

        public bool CanConvertTo(Type type)
        {
            // TODO: Implement when we implement better method overload resolution
            return true;
        }
    }
}
