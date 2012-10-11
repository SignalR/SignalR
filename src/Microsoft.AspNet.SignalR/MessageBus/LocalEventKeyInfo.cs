using System;

namespace Microsoft.AspNet.SignalR
{
    public class LocalEventKeyInfo
    {
        public LocalEventKeyInfo()
        {
            MinLocal = Int32.MaxValue;
        }

        public MessageStore<Message> Store { get; set; }
        public ulong MinLocal { get; set; }
        public int Count { get; set; }
    }
}
