using System;

namespace SignalR.Hubs {
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class HubMethodNameAttribute : Attribute {
        private readonly string methodName;

        public HubMethodNameAttribute(string methodName) {
            if (String.IsNullOrEmpty(methodName)) {
                throw new ArgumentNullException("methodName");
            }
            this.methodName = methodName;
        }

        public string MethodName {
            get { return methodName; }
        }
    }
}