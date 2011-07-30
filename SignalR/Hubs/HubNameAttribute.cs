using System;

namespace SignalR.Hubs {
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class HubNameAttribute : Attribute {
        private readonly string hubName;

        public HubNameAttribute(string hubName) {
            if (String.IsNullOrEmpty(hubName)) {
                throw new ArgumentNullException("hubName");
            }
            this.hubName = hubName;
        }

        public string HubName {
            get { return hubName; }
        }
    }
}
