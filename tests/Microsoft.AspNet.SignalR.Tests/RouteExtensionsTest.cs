using System;
using System.Threading;
using System.Web.Routing;
using Microsoft.AspNet.SignalR.Tests.Owin;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class RouteExtensionsTest
    {
        [Fact]
        public void TestMapHubs()
        {
            var wh = new ManualResetEvent(false);
            RouteCollection routes = new RouteCollection();
            routes.MapHubs("signalr", new HubConfiguration(), appbuilder =>
            {
                appbuilder.Properties[ServerRequestFacts.OwinConstants.HostAppNameKey] = "test";
                wh.Set();
            });
            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(10)));
        }
    }
}
