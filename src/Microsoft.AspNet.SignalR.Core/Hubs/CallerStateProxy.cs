// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Dynamic;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class CallerStateProxy : DynamicObject
    {
        private readonly StateChangeTracker _tracker;

        public CallerStateProxy(StateChangeTracker tracker)
        {
            _tracker = tracker;
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "The compiler generates calls to invoke this")]
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _tracker[binder.Name] = value;
            return true;
        }

        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "The compiler generates calls to invoke this")]
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = _tracker[binder.Name];
            return true;
        }
    }
}
