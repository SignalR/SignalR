// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    public interface IDbExceptionBehavior
    {
        bool IsRecoverableException(Exception exception);
    }
}
