using System;

namespace SignalR.Client.Hubs {
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class HubActionAttribute : Attribute {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        private readonly string _message;

        // This is a positional argument
        public HubActionAttribute(string message) {
            _message = message;
        }

        public string Message {
            get { return _message; }
        }
    }
}
