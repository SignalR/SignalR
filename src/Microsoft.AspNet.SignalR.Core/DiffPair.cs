using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR
{
    internal struct DiffPair<T>
    {
        public bool Reset;
        public ICollection<T> Added;
        public ICollection<T> Removed;
    }
}
