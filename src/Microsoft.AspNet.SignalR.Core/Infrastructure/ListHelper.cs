using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    internal class ListHelper<T>
    {
        public static readonly IList<T> Empty = new List<T>();
    }
}
