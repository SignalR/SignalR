using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR.Infrastructure
{
    /// <summary>
    /// Provides a way for SignalR servers to communicate with each other
    /// </summary>
    public class ServerCommandHandler : IServerCommandHandler
    {
        private readonly IMessageBus _messageBus;
        private readonly IServerIdManager _serverIdManager;
        private readonly IJsonSerializer _serializer;

        // The signal for all signalr servers
        private const string ServerSignal = "__SIGNALR__SERVER__";
        private static readonly string[] ServerSignals = new[] { ServerSignal };

        private string _messageId;
        
        public ServerCommandHandler(IDependencyResolver resolver) :
            this(resolver.Resolve<IMessageBus>(),
                 resolver.Resolve<IServerIdManager>(),
                 resolver.Resolve<IJsonSerializer>())
        {

        }

        public ServerCommandHandler(IMessageBus messageBus, IServerIdManager serverIdManager, IJsonSerializer serializer)
        {
            _messageBus = messageBus;
            _serverIdManager = serverIdManager;
            _serializer = serializer;

            ProcessMessages();
        }

        public Action<ServerCommand> Command
        {
            get;
            set;
        }

        public Task SendCommand(ServerCommand command)
        {
            // Store where the message originated from
            command.ServerId = _serverIdManager.ServerId;

            // Wrap the value so buses that need to serialize can call ToString()
            var wrappedValue = new WrappedValue(command, _serializer);

            // Send the command to the all servers
            return _messageBus.Send(_serverIdManager.ServerId, ServerSignal, wrappedValue);
        }

        private void ProcessMessages()
        {
            // Process messages that come from the bus for servers
            _messageBus.GetMessages(ServerSignals, _messageId, CancellationToken.None)
                       .Then(result =>
                       {
                           // Handle the server commands
                           HandleServerCommands(result.Messages);

                           // Store the last message id
                           _messageId = result.LastMessageId;

                           // Check for more messages
                           ProcessMessages();
                       });
        }

        private void HandleServerCommands(IList<Message> messages)
        {
            foreach (var message in messages)
            {
                // Only handle server commands
                if (ServerSignal.Equals(message.SignalKey))
                {
                    // Uwrap the command and raise the event
                    var command = WrappedValue.Unwrap<ServerCommand>(message.Value, _serializer);
                    OnCommand(command);
                }
            }
        }

        private void OnCommand(ServerCommand command)
        {
            if (Command != null)
            {
                Command(command);
            }
        }
    }
}
