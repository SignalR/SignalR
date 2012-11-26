using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Json
{
    public class JsonFacts
    {
        [Fact]
        public void CamelCaseConversPascalCaseToCamelCase()
        {
            // Act
            string name = Microsoft.AspNet.SignalR.Json.CamelCase("SomeMethod");

            // Assert
            Assert.Equal("someMethod", name);
        }

        [Fact]
        public void MimeTypeReturnsJsonMimeType()
        {
            // Act
            string mimeType = Microsoft.AspNet.SignalR.Json.MimeType;

            // Assert
            Assert.Equal("application/json; charset=UTF-8", mimeType);
        }

        [Fact]
        public void JsonPMimeTypeReturnsJsonPMimeType()
        {
            // Act
            string mimeType = Microsoft.AspNet.SignalR.Json.JsonpMimeType;

            // Assert
            Assert.Equal("text/javascript; charset=UTF-8", mimeType);
        }

        [Fact]
        public void CreateJsonPCallbackWrapsContentInMethod()
        {
            // Act
            string callback = Microsoft.AspNet.SignalR.Json.CreateJsonpCallback("foo", "1");

            // Assert
            Assert.Equal("foo(1);", callback);
        }
    }
}
