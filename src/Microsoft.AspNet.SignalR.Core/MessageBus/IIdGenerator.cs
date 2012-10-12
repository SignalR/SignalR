using System;

namespace Microsoft.AspNet.SignalR
{
    public interface IIdGenerator<T> where T : IComparable<T>
    {
        T GetNext();
        T ConvertFromString(string value);
        string ConvertToString(T value);
    }
}
