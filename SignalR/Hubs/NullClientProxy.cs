using System;
using System.Dynamic;

namespace SignalR.Hubs
{
    internal class NullClientProxy : DynamicObject
    {
        private const string InvalidHubUsageMessage = "Using a hub instance not created by the hub pipeline is unsupported.";

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            throw new InvalidOperationException(InvalidHubUsageMessage);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            throw new InvalidOperationException(InvalidHubUsageMessage);
        }
    }
}
