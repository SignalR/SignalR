// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR
{
    public enum CommandType
    {
        AddToGroup,
        RemoveFromGroup,
        Disconnect,
        Abort,
        Ping
    }
}
