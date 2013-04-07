using Microsoft.AspNet.SignalR.Hubs;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Core.Hubs
{
    public class DefaultJavaScriptProxyGeneratorFacts
    {
        [Fact]
        public void MinifiesScriptUsingConfiguredMinifier()
        {
            // Arrange
            var hubManager = new Mock<IHubManager>();
            var jsMinifier = new Mock<IJavaScriptMinifier>();
            jsMinifier.Setup(m => m.Minify(It.IsAny<string>()))
                .Returns<string>(source => "it was minified");
            var generator = new DefaultJavaScriptProxyGenerator(hubManager.Object, jsMinifier.Object);

            // Act
            var js = generator.GenerateProxy("http://localhost/testhub");

            // Assert
            Assert.Equal("it was minified", js);
        }
    }
}
