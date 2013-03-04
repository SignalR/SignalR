// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Json;

namespace Microsoft.AspNet.SignalR.Knockout
{
    public abstract class KnockoutHub : Hub
    {
        protected virtual Task OnKnockoutUpdate(IJsonValue state)
        {
            return Clients.Others.onKnockoutUpdate(state);
        }
    }
}
