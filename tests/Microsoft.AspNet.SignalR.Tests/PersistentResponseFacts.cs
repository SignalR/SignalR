using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class PersistentResponseFacts
    {
        [Fact]
        public void WriteJsonWritesMessagesThatAreNotExcluded()
        {
            // Arrange
            var writer = new StringWriter();
            var response = new PersistentResponse(m => m.Key == "key2");
            response.Messages = new List<ArraySegment<Message>>();
            response.TransportData = new Dictionary<string, object>();
            response.TransportData["Groups"] = new List<string>
            {
                "g1"
            };
            response.MessageId = "Baz";
            response.Messages.Add(new ArraySegment<Message>(new[] { new Message("1", "key", "value1"), 
                                                                    new Message("1", "key2", "value2") }, 0, 2));

            // Act
            ((IJsonWritable)response).WriteJson(writer);

            // Assert
            Assert.Equal(@"{""MessageId"":""Baz"",""Disconnect"":false,""TimedOut"":false,""TransportData"":{""Groups"":[""g1""]},""Messages"":[value1]}", writer.ToString());
        }

        [Fact]
        public void WriteJsonWritesSkipsCommands()
        {
            // Arrange
            var writer = new StringWriter();
            var response = new PersistentResponse(m => false);
            response.Messages = new List<ArraySegment<Message>>();
            response.TransportData = new Dictionary<string, object>();
            response.TransportData["Groups"] = new List<string>
            {
                "g1"
            };
            response.MessageId = "Baz";
            response.Messages.Add(new ArraySegment<Message>(new[] { new Message("1", "key", "value1") { CommandId = "something"}, 
                                                                    new Message("1", "key2", "value2") }, 0, 2));

            // Act
            ((IJsonWritable)response).WriteJson(writer);

            // Assert
            Assert.Equal(@"{""MessageId"":""Baz"",""Disconnect"":false,""TimedOut"":false,""TransportData"":{""Groups"":[""g1""]},""Messages"":[value2]}", writer.ToString());
        }

        [Fact]
        public void WriteJsonSkipsNullGroups()
        {
            // Arrange
            var writer = new StringWriter();
            var response = new PersistentResponse(m => m.Key == "key2");
            response.TransportData = new Dictionary<string, object>();
            response.Messages = new List<ArraySegment<Message>>();
            response.MessageId = "Baz";
            response.Messages.Add(new ArraySegment<Message>(new[] { new Message("1", "key", "value1"), 
                                                                    new Message("1", "key2", "value2") }, 0, 2));

            // Act
            ((IJsonWritable)response).WriteJson(writer);

            // Assert
            Assert.Equal(@"{""MessageId"":""Baz"",""Disconnect"":false,""TimedOut"":false,""TransportData"":{},""Messages"":[value1]}", writer.ToString());
        }

        [Fact]
        public void WriteJsonSkipsNullTransportDaa()
        {
            // Arrange
            var writer = new StringWriter();
            var response = new PersistentResponse(m => m.Key == "key2");
            response.Messages = new List<ArraySegment<Message>>();
            response.MessageId = "Baz";
            response.Messages.Add(new ArraySegment<Message>(new[] { new Message("1", "key", "value1"), 
                                                                    new Message("1", "key2", "value2") }, 0, 2));

            // Act
            ((IJsonWritable)response).WriteJson(writer);

            // Assert
            Assert.Equal(@"{""MessageId"":""Baz"",""Disconnect"":false,""TimedOut"":false,""Messages"":[value1]}", writer.ToString());
        }
    }
}
