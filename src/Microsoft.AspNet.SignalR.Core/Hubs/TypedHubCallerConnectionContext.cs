// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class TypedHubCallerConnectionContext<T> : TypedHubConnectionContext<T>, IHubCallerConnectionContext<T>
    {
        private IHubCallerConnectionContext<dynamic> _dynamicContext;

        public TypedHubCallerConnectionContext(IHubCallerConnectionContext<dynamic> dynamicContext)
            : base(dynamicContext)
        {
            _dynamicContext = dynamicContext;
        }

        public T Caller
        {
            get
            {
                return TypedClientBuilder<T>.Build(_dynamicContext.Caller);
            }
        }

        public dynamic CallerState
        {
            get
            {
                return _dynamicContext.CallerState;
            }
        }

        public T Others
        {
            get
            {
                return TypedClientBuilder<T>.Build(_dynamicContext.Others);
            }
        }

        public T OthersInGroup(string groupName)
        {
            return TypedClientBuilder<T>.Build(_dynamicContext.OthersInGroup(groupName));
        }

        public T OthersInGroups(IList<string> groupNames)
        {
            return TypedClientBuilder<T>.Build(_dynamicContext.OthersInGroups(groupNames));
        }
    }
}
