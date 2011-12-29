using System;

namespace SignalR
{
    public interface IPersistentConnectionFactory
    {
        PersistentConnection CreateInstance(Type handlerType);
    }
}
