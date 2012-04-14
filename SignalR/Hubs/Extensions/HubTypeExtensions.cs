using System;
using SignalR.Infrastructure;

namespace SignalR.Hubs
{
    internal static class HubTypeExtensions
    {
        internal static string GetHubName(this Type type)
        {
            if (!typeof(IHub).IsAssignableFrom(type))
            {
                return null;
            }

            return ReflectionHelper.GetAttributeValue<HubNameAttribute, string>(type, attr => attr.HubName)
                   ?? type.Name;
        } 
    }
}