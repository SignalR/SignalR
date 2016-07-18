// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;

namespace Microsoft.AspNet.SignalR.Samples.Hubs.Chat
{
    public interface IContentProvider
    {
        string GetContent(HttpWebResponse response);
    }
}