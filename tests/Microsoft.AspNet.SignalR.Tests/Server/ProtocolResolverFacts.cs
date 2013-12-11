using System;
using System.Collections.Specialized;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class ProtocolResolverFacts
    {
        [Theory]
        [InlineData("1.0", "1.5", ".8", "1.0")]
        [InlineData("1.0", "1.5", "1.0", "1.0")]
        [InlineData("1.0", "1.5", "1.2.5", "1.2.5")]
        [InlineData("1.0", "1.5", "1.5", "1.5")]
        [InlineData("1.0", "1.5", "1.9", "1.5")]
        [InlineData("1.0", "1.1", "1.0.5", "1.0.5")]
        [InlineData("1.0", "1.1", "", "1.0")]
        public void ProtocolResolvesCorrectly(string minProtocol, string maxProtocol, string clientProtocol, string expectedProtocol)
        {
            var request = new Mock<IRequest>();
            var queryStrings = new NameValueCollection();
            var minProtocolVersion = new Version(minProtocol);
            var maxProtocolVersion = new Version(maxProtocol);
            var protocolResolver = new ProtocolResolver(minProtocolVersion, maxProtocolVersion);

            queryStrings.Add("clientProtocol", clientProtocol);

            request.Setup(r => r.QueryString).Returns(new NameValueCollectionWrapper(queryStrings));

            var version = protocolResolver.Resolve(request.Object);

            Assert.Equal(version, new Version(expectedProtocol));
        }
    }
}
