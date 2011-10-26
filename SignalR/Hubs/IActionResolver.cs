using System;

namespace SignalR.Hubs
{
    public interface IActionResolver
    {
        ActionInfo ResolveAction(Type hubType, string actionName, object[] parameters);
    }
}
