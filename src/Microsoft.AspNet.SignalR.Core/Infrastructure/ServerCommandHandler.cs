﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    /// <summary>
    /// Default <see cref="IServerCommandHandler"/> implementation.
    /// </summary>
    internal class ServerCommandHandler : IServerCommandHandler, ISubscriber, IDisposable
    {
        private readonly IMessageBus _messageBus;
        private readonly IServerIdManager _serverIdManager;
        private readonly IJsonSerializer _serializer;
        private IDisposable _subscription;

        private const int MaxMessages = 10;

        // The signal for all signalr servers
        private const string ServerSignal = "__SIGNALR__SERVER__";
        private static readonly string[] ServerSignals = new[] { ServerSignal };

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


        public IEnumerable<string> EventKeys
        {
            get
            {
                return ServerSignals;
            }
        }

        event Action<string> ISubscriber.EventKeyAdded
        {
            add
            {
            }
            remove
            {
            }
        }

        event Action<string> ISubscriber.EventKeyRemoved
        {
            add
            {
            }
            remove
            {
            }
        }

        public Func<string> GetCursor { get; set; }

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

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_subscription != null)
                {
                    _subscription.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void ProcessMessages()
        {
            // Process messages that come from the bus for servers
            _subscription = _messageBus.Subscribe(this, cursor: null, callback: HandleServerCommands, maxMessages: MaxMessages);
        }

        private Task<bool> HandleServerCommands(MessageResult result)
        {
            result.Messages.Enumerate(m => ServerSignal.Equals(m.Key),
                                      m =>
                                      {
                                          var command = _serializer.Parse<ServerCommand>(m.Value);
                                          OnCommand(command);
                                      });

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
