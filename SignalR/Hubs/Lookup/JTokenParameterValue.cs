using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SignalR.Hubs
{
    internal class JTokenParameterValue : IParameterValue
    {
        private readonly JToken _value;

        public JTokenParameterValue(JToken value)
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
            return false;
        }
    }
}
