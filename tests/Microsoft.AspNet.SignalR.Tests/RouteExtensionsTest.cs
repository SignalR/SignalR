using System;
using System.Threading;
using System.Web.Routing;
using Microsoft.AspNet.SignalR.FunctionalTests;
using Microsoft.AspNet.SignalR.Tests.Owin;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class RouteExtensionsTest
    {
        [Fact]
        public void TestMapHubs()
        {
            RouteCollection routes = new RouteCollection();
            bool delegateInvoked = false;
            routes.MapHubs("signalr", new HubConfiguration(), appbuilder =>
            {
                appbuilder.Properties[ServerRequestFacts.OwinConstants.HostAppNameKey] = "test";
                delegateInvoked = true;
            });
            
            Assert.True(delegateInvoked);
        }

        [Fact]
        public void TestMapConnectionWithType()
        {
            RouteCollection routes = new RouteCollection();
            bool delegateInvoked = false;
            routes.MapConnection("signalr", "/test", typeof(MyGroupConnection), new ConnectionConfiguration(), appbuilder =>
            {
                appbuilder.Properties[ServerRequestFacts.OwinConstants.HostAppNameKey] = "test";
                delegateInvoked = true;
            });

            Assert.True(delegateInvoked);
        }

        [Fact]
        public void TestMapConnection()
        {
            RouteCollection routes = new RouteCollection();
            bool delegateInvoked = false;
            routes.MapConnection<MyGroupConnection>("signalr", "/test", new ConnectionConfiguration(), appbuilder =>
            {
                appbuilder.Properties[ServerRequestFacts.OwinConstants.HostAppNameKey] = "test";
                delegateInvoked = true;
            });

            Assert.True(delegateInvoked);
        }
    }
}
