using System;

namespace SignalR.Transports
{
    /// <summary>
    /// Represents a connection where we only have the id
    /// </summary>
    internal class ConnectionReference : ITrackingConnection
    {
        public ConnectionReference(string connectionId)
        {
            ConnectionId = connectionId;
        }

        public string ConnectionId
        {
            get;
            private set;
        }

        public bool IsAlive
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsTimedOut
        {
            get { throw new NotImplementedException(); }
        }

        public TimeSpan DisconnectThreshold
        {
            get { throw new NotImplementedException(); }
        }

        public System.Threading.Tasks.Task Disconnect()
        {
            throw new NotImplementedException();
        }

        public void Timeout()
        {
            throw new NotImplementedException();
        }

        public void KeepAlive()
        {
            throw new NotImplementedException();
        }
    }
}
