using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Samples
{
    public class Streaming : PersistentConnection
    {
        protected override IEnumerable<string> OnRejoiningGroups(IRequest request, IEnumerable<string> groups, string connectionId)
        {
            return groups;
        }
    }
}