﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    internal class ConnectingMessageBuffer
    {
        private Queue<JToken> _buffer;
        private IConnection _connection;
        private Action<JToken> _drainCallback;

        public ConnectingMessageBuffer(IConnection connection, Action<JToken> drainCallback)
        {
            _buffer = new Queue<JToken>();
            _connection = connection;
            _drainCallback = drainCallback;
        }

        public bool TryBuffer(JToken message)
        {
            // Check if we need to buffer message
            if (_connection.State == ConnectionState.Connecting)
            {
                _buffer.Enqueue(message);

                return true;
            }

            return false;
        }

        public bool TryDrain()
        {
            // Ensure that the connection is connected when we drain (do not want to drain while a connection is not active)
            if (_connection.State == ConnectionState.Connected)
            {
                while (_buffer.Count != 0)
                {
                    _drainCallback(_buffer.Dequeue());
                }

                return true;
            }

            return false;
        }

        public void Clear()
        {
            _buffer.Clear();
        }
    }
}
