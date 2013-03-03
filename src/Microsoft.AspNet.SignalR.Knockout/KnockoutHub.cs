// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Knockout
{
    public abstract class KnockoutHub : Hub
    {
        public virtual Task OnKnockoutUpdate(dynamic state)
        {
            return Clients.Others.onKnockoutUpdate(state);
        }
    }
}
