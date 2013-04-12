﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.Stress
{
    [Export("MessageBus", typeof(IRun))]
    public class MessageBusRun : RunBase
    {
        private const int MessageBufferSize = 10;
        private readonly MessageBus _bus;

        [ImportingConstructor]
        public MessageBusRun(RunData runData)
            : base(runData)
        {
            _bus = CreateMessageBus();
        }

        protected virtual MessageBus CreateMessageBus()
        {
            return new MessageBus(Resolver);
        }

        protected override IDisposable CreateReceiver(int connectionIndex)
        {
            var subscriber = new Subscriber(connectionIndex.ToString(), new[] { "a", "b", "c" });
            return _bus.Subscribe(subscriber,
                                  cursor: null,
                                  callback: (result, state) => TaskAsyncHelper.True,
                                  maxMessages: MessageBufferSize,
                                  state: null);
        }

        protected override Task Send(int senderIndex)
        {
            return _bus.Publish(senderIndex.ToString(), "a", Payload);
        }

        public override void Dispose()
        {
            _bus.Dispose();

            base.Dispose();
        }
    }
}
