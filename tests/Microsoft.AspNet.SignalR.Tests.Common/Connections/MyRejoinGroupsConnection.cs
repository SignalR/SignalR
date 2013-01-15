using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.FunctionalTests
{
    public class MyRejoinGroupsConnection : MyGroupConnection
    {
        protected override IEnumerable<string> OnRejoiningGroups(IRequest request, IEnumerable<string> groups, string connectionId)
        {
            return groups;
        }
    }
}
