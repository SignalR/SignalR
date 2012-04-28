using Newtonsoft.Json.Linq;

namespace SignalR.Hubs
{
    public static class MethodExtensions
    {
        public static bool Matches(this MethodDescriptor methodDescriptor, params JToken[] parameters)
        {
            if ((methodDescriptor.Parameters.Count > 0 && parameters == null)
                || methodDescriptor.Parameters.Count != parameters.Length)
            {
                return false;
            }

            return true;
        }
    }
}
