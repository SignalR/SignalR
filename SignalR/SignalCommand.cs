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

        internal static bool TryGetCommand(Message message, IJsonSerializer serializer, out SignalCommand command)
        {
            command = null;
            if (!message.SignalKey.EndsWith(SignalrCommand, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            command = message.Value as SignalCommand;

            // Optimization for in memory message store
            if (command != null)
            {
                return true;
            }

            // Otherwise deserialize the message value
            string rawValue = message.Value as string;
            if (rawValue == null)
            {
                return false;
            }

            command = serializer.Parse<SignalCommand>(rawValue);
            return true;
        }

        public CommandType Type { get; set; }
        public object Value { get; set; }
        public TimeSpan? ExpiresAfter { get; set; }
    }
}