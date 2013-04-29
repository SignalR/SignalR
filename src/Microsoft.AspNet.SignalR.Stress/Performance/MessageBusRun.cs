// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

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
        private MessageBus _bus;

        [ImportingConstructor]
        public MessageBusRun(RunData runData)
            : base(runData)
        {
        }

        public override void Initialize()
        {
            _bus = CreateMessageBus();

            base.Initialize();
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

        protected override Task Send(int senderIndex, string source)
        {
            return _bus.Publish(source, "a", Payload);
        }

        protected override void Dispose(bool disposing)
        {
            if (_bus != null && disposing)
            {
                _bus.Dispose();
                _bus = null;
            }

            base.Dispose(disposing);
        }
    }
}
