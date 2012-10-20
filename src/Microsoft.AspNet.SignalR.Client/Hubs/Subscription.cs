// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Hubs
{
    /// <summary>
    /// Represents a subscription to a hub method.
    /// </summary>
    public class Subscription
    {
        public event Action<JToken[]> Data;

        internal void OnData(JToken[] data)
        {
            if (Data != null)
            {
                Data(data);
            }
        } 
    }
}
