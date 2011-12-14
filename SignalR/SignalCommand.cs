using System;

namespace SignalR
{
    public class SignalCommand
    {
        internal const string SignalrCommand = "__SIGNALRCOMMAND__";

        public CommandType Type { get; set; }
        public object Value { get; set; }
        public TimeSpan? ExpiresAfter { get; set; }
    }
}