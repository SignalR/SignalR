using Microsoft.AspNet.SignalR.Hubs;
using Xunit;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Core
{
    public class DefaultHubManagerFacts
    {
        [Fact]
        public void GetValidHub()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);
            var hubDescriptor = hubManager.GetHub("CoreTestHub");

            Assert.NotNull(hubDescriptor);
            Assert.False(hubDescriptor.NameSpecified);
        }

        [Fact]
        public void GetInValidHub()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);
            var hubDescriptor = hubManager.GetHub("__ELLO__");

            Assert.Null(hubDescriptor);
        }
    }
}
