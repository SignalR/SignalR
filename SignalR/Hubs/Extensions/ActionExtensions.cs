using System;
using System.Linq;
using Newtonsoft.Json;
using SignalR.Hubs.Lookup.Descriptors;

namespace SignalR.Hubs.Extensions
{
    public static class ActionExtensions
    {
        public static object[] Adjust(this MethodDescriptor action, params object[] parameters)
        {
            return action.Parameters.Select((p, index) => Bind(parameters[index], p.Type)).ToArray();
        }

        public static bool Matches(this MethodDescriptor action, params object[] parameters)
        {
            if ((action.Parameters.Count == 0 && parameters == null) 
                || action.Parameters.Count != parameters.Length)
            {
                return false;
            }

            return true;
        }

        private static object Bind(object value, Type type)
        {
            if (value == null)
            {
                return null;
            }

            if (value.GetType() == type)
            {
                return value;
            }

            if (type == typeof(Guid))
            {
                return new Guid(value.ToString());
            }

            return JsonConvert.DeserializeObject(value.ToString(), type);
        }
    }
}
