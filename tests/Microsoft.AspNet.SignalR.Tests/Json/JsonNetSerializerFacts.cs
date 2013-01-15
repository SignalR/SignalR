using System.IO;
using Microsoft.AspNet.SignalR.Json;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Json
{
    public class JsonNetSerializerFacts
    {
        [Fact]
        public void SerializeInterceptsIJsonWritable()
        {
            // Arrange
            var serializer = new JsonNetSerializer();
            var sw = new StringWriter();
            var value = new Mock<IJsonWritable>();
            value.Setup(m => m.WriteJson(It.IsAny<TextWriter>()))
                 .Callback<TextWriter>(tw => tw.Write("Custom"));

            // Act
            serializer.Serialize(value.Object, sw);

            // Assert
            Assert.Equal("Custom", sw.ToString());
        }
    }
}
