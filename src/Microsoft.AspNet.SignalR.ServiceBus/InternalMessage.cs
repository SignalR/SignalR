// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    sealed class InternalMessage
    {
        readonly string stream;
        readonly ulong id;
        readonly Message[] messages;

        public InternalMessage(string stream, ulong id, Message[] messages)
        {
            this.stream = stream;
            this.id = id;
            this.messages = messages;
        }

        public Message[] Messages
        {
            get { return this.messages; }
        }

        public string Stream
        {
            get { return this.stream; }
        }

        public ulong Id
        {
            get { return this.id; }
        }
    }
}
