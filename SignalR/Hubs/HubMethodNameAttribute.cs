using System;

namespace SignalR.Hubs
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class HubMethodNameAttribute : Attribute
    {
        public HubMethodNameAttribute(string methodName)
        {
            if (String.IsNullOrEmpty(methodName))
            {
                throw new ArgumentNullException("methodName");
            }
            MethodName = methodName;
        }

        public string MethodName
        {
            get;
            private set;
        }
    }
}