using Microsoft.AspNet.SignalR.Hubs;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class DefaultHubResolverFacts
    {
        [Fact]
        public void ShouldResolveHubByTypeName()
        {
            var resolver = new DefaultDependencyResolver();
            var hubResolver = new ReflectedHubDescriptorProvider(resolver);
            HubDescriptor hub;
            hubResolver.TryGetHub("HubWithoutAttribute", out hub);

            Assert.NotNull(hub);
            Assert.Equal(hub.Name, "HubWithoutAttribute");
            Assert.Equal(hub.NameSpecified, false);
        }

        [Fact]
        public void ShouldResolveHubByHubNameAttribute()
        {
            var resolver = new DefaultDependencyResolver();
            var hubResolver = new ReflectedHubDescriptorProvider(resolver);
            HubDescriptor hub;
            hubResolver.TryGetHub("NameFromAttribute", out hub);

            Assert.NotNull(hub);
            Assert.Equal(hub.Name, "NameFromAttribute");
            Assert.Equal(hub.NameSpecified, true);
        }

        [Fact]
        public void ShouldNotResolveHubByFullTypeName()
        {
            var resolver = new DefaultDependencyResolver();
            var hubResolver = new ReflectedHubDescriptorProvider(resolver);
            HubDescriptor hub;
            hubResolver.TryGetHub("SignalR.Tests.HubWithoutAttribute", out hub);

            Assert.Null(hub);
        }

        [Fact]
        public void ShouldNotResolveHubByTypeNameIfAttributeExists()
        {
            var resolver = new DefaultDependencyResolver();
            var hubResolver = new ReflectedHubDescriptorProvider(resolver);
            HubDescriptor hub;
            hubResolver.TryGetHub("HubWithAttribute", out hub);

            Assert.Null(hub);
        }

        [Fact]
        public void ShouldIgnoreCaseWhenDiscoveringHubs()
        {
            var resolver = new DefaultDependencyResolver();
            var hubResolver = new ReflectedHubDescriptorProvider(resolver);
            HubDescriptor hub;
            hubResolver.TryGetHub("hubwithoutattribute", out hub);

            Assert.NotNull(hub);
            Assert.Equal(hub.Name, "HubWithoutAttribute");
            Assert.Equal(hub.NameSpecified, false);
        }

        [Fact]
        public void ShouldIgnoreCaseWhenDiscoveringHubsUsingManager()
        {
            var resolver = new DefaultDependencyResolver();
            var manager = new DefaultHubManager(resolver);
            var hub = manager.GetHub("hubwithoutattribute");

            Assert.NotNull(hub);
            Assert.Equal(hub.Name, "HubWithoutAttribute");
            Assert.Equal(hub.NameSpecified, false);
        }

        [HubName("NameFromAttribute")]
        public class HubWithAttribute : Hub
        {
        }

        public class HubWithoutAttribute : Hub
        {
        }
    }
}
