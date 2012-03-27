namespace SignalR.Hubs.Extensions
{
    using System;
    using System.Linq;

    using Newtonsoft.Json;

    using SignalR.Hubs.Lookup.Descriptors;

    public static class ActionExtensions
    {
        public static object[] Adjust(this ActionDescriptor action, params object[] parameters)
        {
            var i = 0;
            return action.Parameters.Select(p => Bind(parameters[i++], p.Type)).ToArray();
        }

        public static bool Matches(this ActionDescriptor action, params object[] parameters)
        {
            if ((action.Parameters.Any() && parameters == null) 
                || action.Parameters.Count() != parameters.Length) return false;

            // pszmyd: Added, but commented this out afterwards, as it's a huge overhead...
            /*parameters = action.Adjust(parameters);

            int i = 0;
            foreach (var p in action.Parameters) {
                if (!p.Type.IsInstanceOfType(parameters[i])) return false;
                i++;
            }*/

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