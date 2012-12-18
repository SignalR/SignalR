// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Client.Transports.ServerSentEvents
{
    public class SseEvent
    {
        public SseEvent(EventType type, string data)
        {
            EventType = type;
            Data = data;
        }

        public EventType EventType { get; private set; }
        public string Data { get; private set; }

        public override string ToString()
        {
            return EventType + ": " + Data;
        }

        public static bool TryParse(string line, out SseEvent sseEvent)
        {
            sseEvent = null;

            if (line == null)
            {
                throw new ArgumentNullException("line");
            }

            if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                string data = line.Substring("data:".Length).Trim();
                sseEvent = new SseEvent(EventType.Data, data);
                return true;
            }
            else if (line.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
            {
                string data = line.Substring("id:".Length).Trim();
                sseEvent = new SseEvent(EventType.Id, data);
                return true;
            }

            return false;
        }
    }
}
