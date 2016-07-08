// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Hubs
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class HubNameAttribute : Attribute
    {
        public HubNameAttribute(string hubName)
        {
            if (String.IsNullOrEmpty(hubName))
            {
                throw new ArgumentNullException("hubName");
            }
            HubName = hubName;
        }

        public string HubName
        {
            get;
            private set;
        }
    }
}
