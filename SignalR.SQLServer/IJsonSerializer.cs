using System;

namespace SignalR {
    public interface IJsonSerializer : IJsonStringifier {
        object Parse(string json);

        object Parse(string json, Type targetType);

        T Parse<T>(string json);
    }
}