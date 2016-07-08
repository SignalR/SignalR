// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Dynamic;
using System.Globalization;

namespace Microsoft.AspNet.SignalR.Hubs
{
    internal class NullClientProxy : DynamicObject
    {
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Error_UsingHubInstanceNotCreatedUnsupported));
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.Error_UsingHubInstanceNotCreatedUnsupported));
        }
    }
}
