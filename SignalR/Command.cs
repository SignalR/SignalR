using System;

namespace SignalR
{
    public class Command
    {
        public Command()
        {
            Id = Guid.NewGuid().ToString();
        }

        public bool WaitForAck { get; set; }
        public string Id { get; private set; }
        public CommandType Type { get; set; }
        public string Value { get; set; }
    }
}