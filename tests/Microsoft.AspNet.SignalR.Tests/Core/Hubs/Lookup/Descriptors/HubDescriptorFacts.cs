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
