using System;

namespace SignalR.Infrastructure
{
    public class ServerIdManager : IServerIdManager
    {
        public ServerIdManager()
        {
            ServerId = Guid.NewGuid().ToString();
        }

        public string ServerId
        {
            get;
            private set;
        }
    }
}
