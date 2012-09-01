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
        private const int MaxMessages = 10;

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

        public string Identity
        {
            get
            {
                return _serverIdManager.ServerId;
            }
        }

        public Task SendCommand(ServerCommand command)
        {
            // Store where the message originated from
            command.ServerId = _serverIdManager.ServerId;

            // Send the command to the all servers
            return _messageBus.Publish(_serverIdManager.ServerId, ServerSignal, _serializer.Stringify(command));
        }

        private void ProcessMessages()
        {
            // Process messages that come from the bus for servers
            _messageBus.Subscribe(this, cursor: null, callback: HandleServerCommands, maxMessages: MaxMessages);
        }

        private Task<bool> HandleServerCommands(MessageResult result)
        {
            for (int i = 0; i < result.Messages.Count; i++)
            {
                for (int j = result.Messages[i].Offset; j < result.Messages[i].Offset + result.Messages[i].Count; j++)
                {
                    Message message = result.Messages[i].Array[j];

                    // Only handle server commands
                    if (ServerSignal.Equals(message.Key))
                    {
                        // Uwrap the command and raise the event
                        var command = _serializer.Parse<ServerCommand>(message.Value);
                        OnCommand(command);
                    }
                }
            }

            return TaskAsyncHelper.True;
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
