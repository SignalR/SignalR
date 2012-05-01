namespace SignalR.Infrastructure
{
    public class ServerCommand
    {
        public string ServerId { get; set; }
        public ServerCommandType Type { get; set; }
        public object Value { get; set; }

        public bool IsFromSelf(string serverId)
        {
            return serverId.Equals(ServerId);
        }
    }
}
