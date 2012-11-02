// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Dynamic;

namespace Microsoft.AspNet.SignalR.Hubs
{
    internal class NullClientProxy : DynamicObject
    {
        private const string InvalidHubUsageMessage = Resources.Error_UsingHubInstanceNotCreatedUnsupported;

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
