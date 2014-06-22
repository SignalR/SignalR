
using System;
using System.Text.RegularExpressions;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    public class UrlBuilderFacts
    {
        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("MyConnectionData", "&connectionData=MyConnectionData")]
        public void BuildNegotiateAppendsNoCacheToUrl(string connectionData, string expected)
        {
            Assert.True(
                Regex.Match(UrlBuilder.BuildNegotiate(CreateConnection(), connectionData), 
                "^http://fakeurl/negotiate\\?clientProtocol=1.42" + expected + "&connectionToken=My%20Conn%20Token&noCache=[a-zA-Z0-9-]{36}$")
                    .Success);        
        }

        [Theory]
        [InlineData("42", "&messageId=42")]
        [InlineData("", "&messageId=")]
        [InlineData("4 2", "&messageId=4%202")]
        [InlineData(null, "")]
        public void BuildConnectRetunsValidUrlWithMessageId(string messageId, string expected)
        {
            var connection = CreateConnection();
            Mock.Get(connection).Setup(c => c.MessageId).Returns(messageId);

            Assert.True(
                Regex.Match(UrlBuilder.BuildConnect(connection, "webPolling", null),
                    "^http://fakeurl/connect\\?clientProtocol=1.42&transport=webPolling&connectionToken=My%20Conn%20Token" +
                    expected + "&noCache=[a-zA-Z0-9-]{36}$")
                    .Success);
        }

        [Theory]
        [InlineData("42", "&messageId=42")]
        [InlineData("", "&messageId=")]
        [InlineData("4 2", "&messageId=4%202")]
        [InlineData(null, "")]
        public void BuildReconnectRetunsValidUrlWithMessageId(string messageId, string expected)
        {
            var connection = CreateConnection();
            Mock.Get(connection).Setup(c => c.MessageId).Returns(messageId);

            Assert.True(
                Regex.Match(UrlBuilder.BuildReconnect(connection, "webPolling", null),
                    "^http://fakeurl/reconnect\\?clientProtocol=1.42&transport=webPolling&connectionToken=My%20Conn%20Token" +
                    expected + "&noCache=[a-zA-Z0-9-]{36}$")
                    .Success);
        }

        [Theory]
        [InlineData("42", "&groupsToken=42")]
        [InlineData("", "&groupsToken=")]
        [InlineData("4 2", "&groupsToken=4%202")]
        [InlineData(null, "")]
        public void BuildReconnectRetunsValidUrlWithGroupsToken(string groupsToken, string expected)
        {
            var connection = CreateConnection();
            Mock.Get(connection).Setup(c => c.GroupsToken).Returns(groupsToken);

            Assert.True(
                Regex.Match(UrlBuilder.BuildPoll(connection, "webPolling", null),
                    "^http://fakeurl/poll\\?clientProtocol=1.42&transport=webPolling&connectionToken=My%20Conn%20Token" +
                    expected + "&noCache=[a-zA-Z0-9-]{36}$")
                    .Success);
        }

        private static IConnection CreateConnection(string qs = null)
        {
            var mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.Url).Returns("http://fakeurl/");
            mockConnection.Setup(c => c.Protocol).Returns(new Version(1, 42));
            mockConnection.Setup(c => c.ConnectionToken).Returns("My Conn Token");
            mockConnection.Setup(c => c.QueryString).Returns(qs);
            return mockConnection.Object;
        }

    }
}
