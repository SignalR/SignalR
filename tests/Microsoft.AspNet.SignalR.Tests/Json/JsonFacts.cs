using Microsoft.AspNet.SignalR.Json;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Json
{
    public class JsonFacts
    {
        [Fact]
        public void CamelCaseConversPascalCaseToCamelCase()
        {
            // Act
            string name = JsonUtility.CamelCase("SomeMethod");

            // Assert
            Assert.Equal("someMethod", name);
        }

        [Fact]
        public void MimeTypeReturnsJsonMimeType()
        {
            // Act
            string mimeType = JsonUtility.MimeType;

            // Assert
            Assert.Equal("application/json; charset=UTF-8", mimeType);
        }

        [Fact]
        public void JsonPMimeTypeReturnsJsonPMimeType()
        {
            // Act
            string mimeType = JsonUtility.JsonpMimeType;

            // Assert
            Assert.Equal("text/javascript; charset=UTF-8", mimeType);
        }

        [Fact]
        public void CreateJsonPCallbackWrapsContentInMethod()
        {
            // Act
            string callback = JsonUtility.CreateJsonpCallback("foo", "1");

            // Assert
            Assert.Equal("foo(1);", callback);
        }
    }
}
