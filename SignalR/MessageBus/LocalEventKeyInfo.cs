using System;

namespace SignalR
{
    public class LocalEventKeyInfo
    {
        public LocalEventKeyInfo()
        {
            MinLocal = Int32.MaxValue;
        }

        public Topic Topic { get; set; }
        public ulong MinLocal { get; set; }
        public int Count { get; set; }
    }
}
