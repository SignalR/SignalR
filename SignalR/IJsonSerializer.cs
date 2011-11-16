using System;

namespace SignalR
{
    public interface IJsonSerializer
    {
        string Stringify(object value);

        object Parse(string json);

        object Parse(string json, Type targetType);

        T Parse<T>(string json);
    }
}