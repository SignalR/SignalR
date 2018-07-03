// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class UnusableProtectedConnection : PersistentConnection
    {
        protected override bool AuthorizeRequest(IRequest request)
        {
            return false;
        }
    }
}
