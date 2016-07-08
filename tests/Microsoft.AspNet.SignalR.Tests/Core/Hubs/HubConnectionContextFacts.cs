// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.SignalR.Hubs;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Core.Hubs
{
    public class HubConnectionContextFacts
    {
        [Fact]
        public void GroupThrowsNullExceptionWhenGroupNameIsNull()
        {
            var hubConContext = new HubConnectionContext();
            Assert.Throws<ArgumentException>(() => hubConContext.Group(null));
        }

        [Fact]
        public void ClientThrowsNullExceptionWhenClientIdIsNull()
        {
            var hubConContext = new HubConnectionContext();
            Assert.Throws<ArgumentException>(() => hubConContext.Client(null));
        }
    }
}
