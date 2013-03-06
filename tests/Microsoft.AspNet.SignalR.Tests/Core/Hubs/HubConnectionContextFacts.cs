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

            try
            {
                hubConContext.Group(null);
            }
            catch (Exception ex)
            {
                Assert.IsType(typeof(ArgumentNullException), ex);
            }
        }

        [Fact]
        public void ClientThrowsNullExceptionWhenClientIdIsNull()
        {
            var hubConContext = new HubConnectionContext();

            try
            {
                hubConContext.Client(null);
            }
            catch (Exception ex)
            {
                Assert.IsType(typeof(ArgumentNullException), ex);
            }
        }
    }
}
