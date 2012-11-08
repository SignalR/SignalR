using System;
using Microsoft.AspNet.SignalR.Hubs;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Core
{
    public class HubManagerExtensionsFacts
    {
        [Fact]
        public void EnsureHubThrowsWhenCantResolve()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);

            Assert.Throws<InvalidOperationException>(() => hubManager.EnsureHub("__ELLO__"));
        }

        [Fact]
        public void EnsureHubSuccessfullyResolves()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);
            var TestHubName = "CoreTestHubWithMethod";

            HubDescriptor hub = null,
                          actualDescriptor = hubManager.GetHub(TestHubName);

            Assert.DoesNotThrow(() =>
            {
                hub = hubManager.EnsureHub(TestHubName);
            });

            Assert.Equal(hub, actualDescriptor);
        }
    }
}
