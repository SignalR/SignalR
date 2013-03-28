// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Messaging
{
    internal class ScaleoutStreamManager
    {
        private readonly Func<int, IList<Message>, Task> _send;
        private readonly Action<int, ulong, IList<Message>> _receive;
        private readonly ScaleoutTaskQueue[] _sendQueues;

        public ScaleoutStreamManager(Func<int, IList<Message>, Task> send,
                                     Action<int, ulong, IList<Message>> receive,
                                     int streamCount)
        {
            _sendQueues = new ScaleoutTaskQueue[streamCount];
            _send = send;
            _receive = receive;

            var receiveMapping = new ScaleoutMappingStore[streamCount];

            for (int i = 0; i < streamCount; i++)
            {
                _sendQueues[i] = new ScaleoutTaskQueue();
                receiveMapping[i] = new ScaleoutMappingStore();
            }

            Streams = new ReadOnlyCollection<ScaleoutMappingStore>(receiveMapping);
        }

        public IList<ScaleoutMappingStore> Streams { get; private set; }

        public void Open(int streamIndex)
        {
            _sendQueues[streamIndex].Open();
        }

        public void Buffer(int streamIndex)
        {
            _sendQueues[streamIndex].Buffer();   
        }

        public Task Send(int streamIndex, IList<Message> messages)
        {
            var context = new SendContext(this, streamIndex, messages);

            return _sendQueues[streamIndex].Enqueue(state => Send(state), context);
        }

        public void OnReceived(int streamIndex, ulong id, IList<Message> messages)
        {
            _receive(streamIndex, id, messages);
            
            // We assume if a message has come in then the stream is open
            Open(streamIndex);
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
    }
}
