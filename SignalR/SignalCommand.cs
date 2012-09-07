using System;

namespace SignalR
{
    public class SignalCommand
    {
        public CommandType Type { get; set; }
        public string Value { get; set; }
    }
}