namespace SignalR.Infrastructure
{
    /// <summary>
    /// A server to server command.
    /// </summary>
    public class ServerCommand
    {
        /// <summary>
        /// Gets or sets the id of the command where this message originated from.
        /// </summary>
        public string ServerId { get; set; }

        /// <summary>
        /// Gets of sets the command type.
        /// </summary>
        public ServerCommandType Type { get; set; }

        /// <summary>
        /// Gets or sets the value for this command.
        /// </summary>
        public object Value { get; set; }

        public bool IsFromSelf(string serverId)
        {
            return serverId.Equals(ServerId);
        }
    }
}
