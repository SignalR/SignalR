// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.SignalR.Hubs;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Core
{
    public class HubDescriptorFacts
    {
        [Fact]
        public void CorrectQualifiedName()
        {
            string hubName = "MyHubDescriptor",
                   unqualifiedName = "MyUnqualifiedName";

            HubDescriptor hubDescriptor = new HubDescriptor()
            {
                Name = hubName
            };

            Assert.Equal(hubDescriptor.CreateQualifiedName(unqualifiedName), hubName + "." + unqualifiedName);
        }
    }
}
