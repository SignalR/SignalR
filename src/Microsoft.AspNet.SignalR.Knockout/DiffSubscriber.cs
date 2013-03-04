using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Messaging;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Knockout
{
    public class DiffSubscriber : ISubscriber
    {
        private const int _maxMessages = 10;
        private readonly string _signal;
        private readonly IMessageBus _bus;
        private readonly IJsonSerializer _serailizer;

        public DiffSubscriber(IMessageBus bus, IJsonSerializer serializer, string signal)
        {
            _bus = bus;
            _serailizer = serializer;
            _signal = signal;
            EventKeys = new[] { signal };
            Identity = "Knockout DiffSubscriber: " + Guid.NewGuid();
        }

        public IList<string> EventKeys { get; private set; }

        public Action<TextWriter> WriteCursor { get; set; }

        public string Identity { get; private set; }

        public event Action<ISubscriber, string> EventKeyAdded;

        public event Action<ISubscriber, string> EventKeyRemoved;

        public Subscription Subscription { get; set; }

        public IDisposable Start(Func<string, JRaw, Task> callback)
        {
            return _bus.Subscribe(this,
                                  null,
                                  ProcessResults(callback),
                                  _maxMessages,
                                  null);
        }

        // But who will think of the Func allocations!?
        private Func<MessageResult, object, Task<bool>> ProcessResults(Func<string, JRaw, Task> callback)
        {
            return (messageResult, subscribeState) =>
            {
                var processTask = TaskAsyncHelper.Empty;

                messageResult.Messages.Enumerate<object>(message => !message.IsCommand, (s, m) =>
                {
                    processTask = processTask.ContinueWith((finishedTask, messageState) =>
                    {
                        // Continue even if faulted
                        var message = (Message)messageState;
                        var diff = _serailizer.Parse<JRaw>(message.Value, message.Encoding);
                        return callback(message.Source, diff).Catch();
                    }, m).FastUnwrap();
                }, null);

                return processTask.ContinueWith(pt => true);
            };
        }
    }
}
