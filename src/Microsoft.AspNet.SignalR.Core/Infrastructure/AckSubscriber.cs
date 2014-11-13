// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    /// <summary>
    /// A singleton that subscribes to all ACKs sent over the
    /// <see cref="Microsoft.AspNet.SignalR.Messaging.IMessageBus"/> and
    /// triggers any corresponding ACKs on the <see cref="IAckHandler"/>.
    /// </summary>
    internal class AckSubscriber : ISubscriber, IDisposable
    {
        private readonly IMessageBus _messageBus;
        private readonly IAckHandler _ackHandler;
        private IDisposable _subscription;

        private const int MaxMessages = 10;

        private static readonly string[] ServerSignals = new[] { Signal };

        public AckSubscriber(IDependencyResolver resolver) :
            this(resolver.Resolve<IMessageBus>(),
                 resolver.Resolve<IAckHandler>())
        {
        }

        public AckSubscriber(IMessageBus messageBus, IAckHandler ackHandler)
        {
            _messageBus = messageBus;
            _ackHandler = ackHandler;

            Identity = Guid.NewGuid().ToString();

            ProcessMessages();
        }

        // The signal for all signalr servers
        public const string Signal = "__SIGNALR__SERVER__";

        public IList<string> EventKeys
        {
            get { return ServerSignals; }
        }

        public event Action<ISubscriber, string> EventKeyAdded
        {
            add { }
            remove { }
        }

        public event Action<ISubscriber, string> EventKeyRemoved
        {
            add { }
            remove { }
        }

        public Action<TextWriter> WriteCursor { get; set; }

        public string Identity { get; private set; }

        public Subscription Subscription { get; set; }

        public void Dispose()
        {
            if (_subscription != null)
            {
                _subscription.Dispose();
            }
        }

        private void ProcessMessages()
        {
            // Process messages that come from the bus for servers
            _subscription = _messageBus.Subscribe(this, cursor: null, callback: TriggerAcks, maxMessages: MaxMessages, state: null);
        }

        private Task<bool> TriggerAcks(MessageResult result, object state)
        {
            result.Messages.Enumerate<object>(m => m.IsAck,
                                              (s, m) => ((IAckHandler)s).TriggerAck(m.CommandId),
                                              state: _ackHandler);

            return TaskAsyncHelper.True;
        }
   }
}
