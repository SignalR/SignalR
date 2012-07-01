using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR.Infrastructure
{
    /// <summary>
    /// Default <see cref="IServerCommandHandler"/> implementation.
    /// </summary>
    public class ServerCommandHandler : IServerCommandHandler, ISubscriber
    {
        private readonly INewMessageBus _messageBus;
        private readonly IServerIdManager _serverIdManager;
        private readonly IJsonSerializer _serializer;

        // The signal for all signalr servers
        private const string ServerSignal = "__SIGNALR__SERVER__";
        private static readonly string[] ServerSignals = new[] { ServerSignal };

        public ServerCommandHandler(IDependencyResolver resolver) :
            this(resolver.Resolve<INewMessageBus>(),
                 resolver.Resolve<IServerIdManager>(),
                 resolver.Resolve<IJsonSerializer>())
        {

        }

        public ServerCommandHandler(INewMessageBus messageBus, IServerIdManager serverIdManager, IJsonSerializer serializer)
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


        public IEnumerable<string> EventKeys
        {
            get
            {
                return ServerSignals;
            }
        }

        public event Action<string, string> EventAdded;

        public event Action<string> EventRemoved;

        public Task SendCommand(ServerCommand command)
        {
            // Store where the message originated from
            command.ServerId = _serverIdManager.ServerId;

            // Wrap the value so buses that need to serialize can call ToString()
            var wrappedValue = new WrappedValue(command, _serializer);

            // Send the command to the all servers
            return _messageBus.Publish(_serverIdManager.ServerId, ServerSignal, wrappedValue);
        }

        private void ProcessMessages()
        {
            // Process messages that come from the bus for servers
            _messageBus.Subscribe(this, cursor: null, callback: HandleServerCommands);
        }

        private Task HandleServerCommands(Exception ex, MessageResult result)
        {
            foreach (var message in result.Messages)
            {
                // Only handle server commands
                if (ServerSignal.Equals(message.SignalKey))
                {
                    // Uwrap the command and raise the event
                    var command = WrappedValue.Unwrap<ServerCommand>(message.Value, _serializer);
                    OnCommand(command);
                }
            }

            return TaskAsyncHelper.Empty;
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
