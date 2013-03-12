using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Messaging
{
    internal class ScaleoutStreamManager
    {
        private readonly Func<int, IList<Message>, Task> _send;
        private readonly Func<int, ulong, IList<Message>, Task> _receive;
        private readonly ScaleoutTaskQueue[] _sendQueues;
        private readonly TaskQueue _receiveQueue;

        public ScaleoutStreamManager(TraceSource trace,
                                    Func<int, IList<Message>, Task> send,
                                    Func<int, ulong, IList<Message>, Task> receive,
                                    int streamCount)
        {
            _sendQueues = new ScaleoutTaskQueue[streamCount];
            _send = send;
            _receive = receive;
            _receiveQueue = new TaskQueue();

            var receiveMapping = new IndexedDictionary[streamCount];

            for (int i = 0; i < streamCount; i++)
            {
                _sendQueues[i] = new ScaleoutTaskQueue(trace);
                receiveMapping[i] = new IndexedDictionary();
            }

            Streams = new ReadOnlyCollection<IndexedDictionary>(receiveMapping);
        }

        public IList<IndexedDictionary> Streams { get; private set; }

        public void Open(int streamIndex)
        {
            _sendQueues[streamIndex].Open();
        }

        public void Close(int streamIndex, Exception exception)
        {
            _sendQueues[streamIndex].Close(exception);
        }

        public void Buffer(int streamIndex, int bufferSize)
        {
            _sendQueues[streamIndex].Buffer(bufferSize);
        }

        public Task Send(int streamIndex, IList<Message> messages)
        {
            var context = new SendContext(this, streamIndex, messages);

            return _sendQueues[streamIndex].Enqueue(state => Send(state), context);
        }

        public Task OnReceived(int streamIndex, ulong id, IList<Message> messages)
        {
            var context = new ReceiveContext(this, streamIndex, id, messages);

            return _receiveQueue.Enqueue(state => OnReceived(state), context);
        }

        private static Task OnReceived(object state)
        {
            var context = (ReceiveContext)state;

            return context.QueueManager._receive(context.Index, context.Id, context.Messages);
        }

        private static Task Send(object state)
        {
            var context = (SendContext)state;

            return context.QueueManager._send(context.Index, context.Messages);
        }

        private class SendContext
        {
            public ScaleoutStreamManager QueueManager;
            public int Index;
            public IList<Message> Messages;

            public SendContext(ScaleoutStreamManager scaleoutStream, int index, IList<Message> messages)
            {
                QueueManager = scaleoutStream;
                Index = index;
                Messages = messages;
            }
        }

        private class ReceiveContext
        {
            public ScaleoutStreamManager QueueManager;
            public int Index;
            public ulong Id;
            public IList<Message> Messages;

            public ReceiveContext(ScaleoutStreamManager scaleoutStream, int index, ulong id, IList<Message> messages)
            {
                QueueManager = scaleoutStream;
                Index = index;
                Id = id;
                Messages = messages;
            }
        }
    }
}
