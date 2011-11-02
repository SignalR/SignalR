using System;

namespace SignalR.Hubs
{
    public interface IHubTypeResolver
    {
        Type ResolveType(string hubName);
    }
}
