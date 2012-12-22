using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    internal struct DiffPair<T>
    {
        public bool Reset;
        public ICollection<T> Added;
        public ICollection<T> Removed;
    }
}
