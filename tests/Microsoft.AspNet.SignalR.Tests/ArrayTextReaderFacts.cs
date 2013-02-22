using System;
using System.IO;
using System.Text;
using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class ArrayTextReaderFacts
    {
        [Fact]
        public void CanDeserializeJson()
        {
            var json = JsonConvert.SerializeObject(new { A = 1, B = 2, C = "Hello World", D = new string('C', 10000) });
            TextReader reader = GetReaderFor(json);

            var serializer = new JsonSerializer();
            var obj = (JObject)serializer.Deserialize(new JsonTextReader(reader));

            Assert.Equal(1, obj.Value<int>("A"));
            Assert.Equal(2, obj.Value<int>("B"));
            Assert.Equal("Hello World", obj.Value<string>("C"));
            Assert.Equal(new string('C', 10000), obj.Value<string>("D"));
        }

        private TextReader GetReaderFor(string value)
        {
            var encoding = new UTF8Encoding();
            var buffer = new ArraySegment<byte>(encoding.GetBytes(value));
            return new ArraySegmentTextReader(buffer, encoding);
        }
    }
}
