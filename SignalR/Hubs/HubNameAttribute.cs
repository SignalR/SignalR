using System;

namespace SignalR.Hubs
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class HubNameAttribute : Attribute
    {
        public HubNameAttribute(string hubName)
        {
            if (String.IsNullOrEmpty(hubName))
            {
                throw new ArgumentNullException("hubName");
            }
            HubName = hubName;
        }

        public string HubName
        {
            get;
            private set;
        }
    }
}
