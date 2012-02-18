using System;
using Newtonsoft.Json;

namespace SignalR
{
    public class JsonConvertAdapter : IJsonSerializer
    {
        public string Stringify(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public object Parse(string json)
        {
            return JsonConvert.DeserializeObject(json);
        }

        public object Parse(string json, Type targetType)
        {
            return JsonConvert.DeserializeObject(json, targetType);
        }

        public T Parse<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}