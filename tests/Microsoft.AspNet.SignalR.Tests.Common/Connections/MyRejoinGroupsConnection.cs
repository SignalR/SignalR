using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.FunctionalTests
{
    public class MyRejoinGroupsConnection : MyGroupConnection
    {
        protected override IList<string> OnRejoiningGroups(IRequest request, IList<string> groups, string connectionId)
        {
            return groups;
        }
    }
}
