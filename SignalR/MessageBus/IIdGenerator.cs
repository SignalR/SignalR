using System;

namespace SignalR.MessageBus
{
    public interface IIdGenerator<T> where T : IComparable<T>
    {
        T GetNext();
        T ConvertFromString(string value);
        string ConvertToString(T value);
    }
}
