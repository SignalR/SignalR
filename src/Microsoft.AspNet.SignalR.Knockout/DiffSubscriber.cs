// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Messaging;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Knockout
{
    public class DiffSubscriber : ISubscriber, IDisposable
    {
        private const int _maxMessages = 10;
        private readonly string _signal;
        private readonly IMessageBus _bus;
        private readonly IJsonSerializer _serailizer;
        private readonly IDisposable _subscriptionDisposer;

        public DiffSubscriber(IMessageBus bus,
                              IJsonSerializer serializer,
                              string signal,
                              Func<string, JRaw, Task> diffHandler,
                              Func<Message, Task> commandHandler)
        {
            _bus = bus;
            _serailizer = serializer;
            _signal = signal;
            EventKeys = new[] { signal };
            Identity = "Knockout DiffSubscriber: " + Guid.NewGuid();
            _subscriptionDisposer = _bus.Subscribe(this,
                                                   null,
                                                   ProcessResults(diffHandler, commandHandler),
                                                   _maxMessages,
                                                   null);
        }

        public IList<string> EventKeys { get; private set; }

        public Action<TextWriter> WriteCursor { get; set; }

        public string Identity { get; private set; }

        public event Action<ISubscriber, string> EventKeyAdded;

        public event Action<ISubscriber, string> EventKeyRemoved;

        public Subscription Subscription { get; set; }

        // But who will think of the Func allocations!?
        private Func<MessageResult, object, Task<bool>> ProcessResults(Func<string, JRaw, Task> diffHandler,
                                                                       Func<Message, Task> commandHandler)
        {
            return (messageResult, subscribeState) =>
            {
                var processTask = TaskAsyncHelper.Empty;

                messageResult.Messages.Enumerate<object>(message => true, (s, m) =>
                {
                    processTask = processTask.ContinueWith((finishedTask, messageState) =>
                    {
                        // Continue even if faulted
                        var message = (Message)messageState;

                        if (!message.IsCommand)
                        {
                            var diff = _serailizer.Parse<JRaw>(message.Value, message.Encoding);
                            return diffHandler(message.Source, diff).Catch();
                        }
                        else
                        {
                            return commandHandler(message).Catch();
                        }

                    }, m).FastUnwrap();
                }, null);

                return processTask.ContinueWith(pt => true);
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _subscriptionDisposer.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
