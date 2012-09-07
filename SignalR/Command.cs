using System;

namespace SignalR
{
    public class Command
    {
        public CommandType Type { get; set; }
        public string Value { get; set; }
    }
}