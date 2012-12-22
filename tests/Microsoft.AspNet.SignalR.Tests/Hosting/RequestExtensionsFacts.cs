using System.Collections.Specialized;
using Moq;
using Xunit;
using Microsoft.AspNet.SignalR.Hosting;

namespace Microsoft.AspNet.SignalR.Tests.Hosting
{
    public class RequestExtensionsFacts
    {
        public class QueryStringOrForm
        {
            [Fact]
            public void GetsValueFromFormIfQueryStringIsNull()
            {
                // Arrange
                var request = new Mock<IRequest>();
                request.Setup(m => m.QueryString).Returns(new NameValueCollection());
                var form = new NameValueCollection();
                form["value"] = "1";
                request.Setup(m => m.Form).Returns(form);

                // Act
                string value = request.Object.QueryStringOrForm("value");

                // Assert
                Assert.Equal("1", value);
            }

            [Fact]
            public void GetsValueQueryStringIfNotNull()
            {
                // Arrange
                var request = new Mock<IRequest>();
                var qs = new NameValueCollection();
                qs["value"] = "1";
                request.Setup(m => m.QueryString).Returns(qs);
                request.Setup(m => m.Form).Returns(new NameValueCollection());

                // Act
                string value = request.Object.QueryStringOrForm("value");

                // Assert
                Assert.Equal("1", value);
            }

            [Fact]
            public void GetsValueQueryStringIfInBoth()
            {
                // Arrange
                var request = new Mock<IRequest>();
                var qs = new NameValueCollection();
                qs["value"] = "1";
                request.Setup(m => m.QueryString).Returns(qs);
                var form = new NameValueCollection();
                form["value"] = "2";
                request.Setup(m => m.Form).Returns(form);

                // Act
                string value = request.Object.QueryStringOrForm("value");

                // Assert
                Assert.Equal("1", value);
            }
        }
    }
}
