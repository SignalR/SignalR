using System.Reflection;

namespace SignalR.Hubs
{
    public class ActionInfo
    {
        public object[] Arguments { get; set; }
        public MethodInfo Method { get; set; }
    }
}
