using System.Web.Script.Serialization;

namespace SignalR
{
    public class JavaScriptSerializerAdapter : IJsonStringifier
    {
        private JavaScriptSerializer _serializer;

        public JavaScriptSerializerAdapter(JavaScriptSerializer serializer)
        {
            _serializer = serializer;
        }

        public string Stringify(object obj)
        {
            return _serializer.Serialize(obj);
        }
    }
}