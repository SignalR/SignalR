using System;
using System.Web.Script.Serialization;

namespace SignalR
{
    public class JavaScriptSerializerAdapter : IJsonSerializer
    {
        private readonly JavaScriptSerializer _serializer;

        public JavaScriptSerializerAdapter(JavaScriptSerializer serializer)
        {
            _serializer = serializer;
        }

        public string Stringify(object obj)
        {
            return _serializer.Serialize(obj);
        }

        public object Parse(string json)
        {
            return _serializer.DeserializeObject(json);
        }

        public object Parse(string json, Type targetType)
        {
            return _serializer.Deserialize(json, targetType);
        }

        public T Parse<T>(string json)
        {
            return _serializer.Deserialize<T>(json);
        }
    }
}