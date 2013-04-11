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
