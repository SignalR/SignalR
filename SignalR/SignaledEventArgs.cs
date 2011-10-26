using System;

namespace SignalR
{
    public class SignaledEventArgs : EventArgs
    {
        public string EventKey { get; private set; }

        public SignaledEventArgs(string eventKey)
        {
            EventKey = eventKey;
        }
    }
}
