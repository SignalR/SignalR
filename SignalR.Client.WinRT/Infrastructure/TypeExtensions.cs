using System;

namespace SignalR.Client.Infrastructure
{
    public static class TypeExtensions
    {
        public static bool IsAssignableFrom(this Type type, Type c)
        {
            // TODO: Figure out how to make this work
            return type == c;
        }
    }
}
