﻿using Microsoft.AspNet.SignalR.Hubs;
using Xunit;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Core
{
    public class MethodExtensionsFacts
    {
        [Fact]
        public void MatchSuccessful()
        {
            var resolver = new DefaultDependencyResolver();
            var hubManager = new DefaultHubManager(resolver);

            // Should be AddNumbers
            MethodDescriptor methodDescriptor = hubManager.GetHubMethod("CoreTestHubWithMethod", "AddNumbers", null, null);

            // We should find our method descriptor
            Assert.NotNull(methodDescriptor);

            // Value does not matter, hence the null;
            Assert.True(methodDescriptor.Matches(null, null));
        }
    }
}
