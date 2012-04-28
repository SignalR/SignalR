using System;

namespace SignalR
{
    public class SignalCommand
    {
        private const string SignalrCommand = "__SIGNALRCOMMAND__";

        internal static string AddCommandSuffix(string eventKey)
        {
            return eventKey + "." + SignalrCommand;
        }

        public static bool IsCommand(Message message)
        {
            return message.SignalKey.EndsWith(SignalrCommand, StringComparison.OrdinalIgnoreCase);
        }

        public CommandType Type { get; set; }
        public object Value { get; set; }
    }
}