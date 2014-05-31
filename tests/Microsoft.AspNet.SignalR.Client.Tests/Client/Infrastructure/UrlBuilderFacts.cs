
using System;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    public class UrlBuilderFacts
    {
        public class BuildNegotiate
        {
            [Theory]
            [InlineData(null, "")]
            [InlineData("", "")]
            [InlineData("MyConnectionData", "&connectionData=MyConnectionData")]
            public void BuildNegotiateReturnsValidUrlWithConnectionData(string connectionData, string expected)
            {
                Assert.Equal(
                    "http://fakeurl/negotiate?clientProtocol=1.42" + expected + "&connectionToken=My%20Conn%20Token",
                    UrlBuilder.BuildNegotiate(CreateConnection(), connectionData));
            }

            [Theory]
            [InlineData("bob=12345", "&bob=12345")]
            [InlineData("bob=12345&foo=leet&baz=laskjdflsdk", "&bob=12345&foo=leet&baz=laskjdflsdk")]
            [InlineData("", "")]
            [InlineData(null, "")]
            [InlineData("?foo=bar", "&foo=bar")]
            [InlineData("?foo=bar&baz=bear", "&foo=bar&baz=bear")]
            [InlineData("&foo=bar", "&foo=bar")]
            [InlineData("&foo=bar&baz=bear", "&foo=bar&baz=bear")]
            public void BuildNegotiateReturnsValidUrlWithCustomQueryString(string qs, string expected)
            {
                Assert.Equal("http://fakeurl/negotiate?clientProtocol=1.42&connectionToken=My%20Conn%20Token" + expected,
                    UrlBuilder.BuildNegotiate(CreateConnection(qs), null));
            }
        }

        public class BuildStart
        {
            [Theory]
            [InlineData(null, "")]
            [InlineData("", "")]
            [InlineData("MyConnectionData", "&connectionData=MyConnectionData")]
            public void BuildStartRetunsValidUrlWithConnectionData(string connectionData, string expected)
            {
                Assert.Equal(
                    "http://fakeurl/start?clientProtocol=1.42&transport=webPolling" + expected + "&connectionToken=My%20Conn%20Token",
                    UrlBuilder.BuildStart(CreateConnection(), "webPolling", connectionData));
            }

            [Theory]
            [InlineData("bob=12345", "&bob=12345")]
            [InlineData("bob=12345&foo=leet&baz=laskjdflsdk", "&bob=12345&foo=leet&baz=laskjdflsdk")]
            [InlineData("", "")]
            [InlineData(null, "")]
            [InlineData("?foo=bar", "&foo=bar")]
            [InlineData("?foo=bar&baz=bear", "&foo=bar&baz=bear")]
            [InlineData("&foo=bar", "&foo=bar")]
            [InlineData("&foo=bar&baz=bear", "&foo=bar&baz=bear")]
            public void BuildStartRetunsValidUrlWithConnectionDataAndCustomQueryString(string qs, string expected)
            {
                Assert.Equal(
                    "http://fakeurl/start?clientProtocol=1.42&transport=webPolling&connectionData=CustomConnectionData&connectionToken=My%20Conn%20Token" + expected,
                    UrlBuilder.BuildStart(CreateConnection(qs), "webPolling", "CustomConnectionData"));
            }
        }

        public class BuildAbort
        {
            [Theory]
            [InlineData(null, "")]
            [InlineData("", "")]
            [InlineData("MyConnectionData", "&connectionData=MyConnectionData")]
            public void BuildAbortRetunsValidUrlWithConnectionData(string connectionData, string expected)
            {
                Assert.Equal(
                    "http://fakeurl/abort?clientProtocol=1.42&transport=webPolling" + expected + "&connectionToken=My%20Conn%20Token",
                    UrlBuilder.BuildAbort(CreateConnection(), "webPolling", connectionData));
            }

            [Theory]
            [InlineData("bob=12345", "&bob=12345")]
            [InlineData("bob=12345&foo=leet&baz=laskjdflsdk", "&bob=12345&foo=leet&baz=laskjdflsdk")]
            [InlineData("", "")]
            [InlineData(null, "")]
            [InlineData("?foo=bar", "&foo=bar")]
            [InlineData("?foo=bar&baz=bear", "&foo=bar&baz=bear")]
            [InlineData("&foo=bar", "&foo=bar")]
            [InlineData("&foo=bar&baz=bear", "&foo=bar&baz=bear")]
            public void BuildAbortRetunsValidUrlWithConnectionDataAndCustomQueryString(string qs, string expected)
            {
                Assert.Equal(
                    "http://fakeurl/abort?clientProtocol=1.42&transport=webPolling&connectionData=CustomConnectionData&connectionToken=My%20Conn%20Token" +
                    expected,
                    UrlBuilder.BuildAbort(CreateConnection(qs), "webPolling", "CustomConnectionData"));
            }
        }

        public class BuildConnect
        {
            [Theory]
            [InlineData("42", "&messageId=42")]
            [InlineData("", "&messageId=")]
            [InlineData("4 2", "&messageId=4%202")]
            [InlineData(null, "")]
            public void BuildConnectRetunsValidUrlWithMessageId(string messageId, string expected)
            {
                var connection = CreateConnection();
                Mock.Get(connection).Setup(c => c.MessageId).Returns(messageId);

                Assert.Equal(
                    "http://fakeurl/connect?clientProtocol=1.42&transport=webPolling&connectionToken=My%20Conn%20Token" +
                    expected,
                    UrlBuilder.BuildConnect(connection, "webPolling", null));
            }

            [Theory]
            [InlineData("42", "&groupsToken=42")]
            [InlineData("", "&groupsToken=")]
            [InlineData("4 2", "&groupsToken=4%202")]
            [InlineData(null, "")]
            public void BuildConnectRetunsValidUrlWithGroupsToken(string groupsToken, string expected)
            {
                var connection = CreateConnection();
                Mock.Get(connection).Setup(c => c.GroupsToken).Returns(groupsToken);

                Assert.Equal(
                    "http://fakeurl/connect?clientProtocol=1.42&transport=webPolling&connectionToken=My%20Conn%20Token" +
                    expected,
                    UrlBuilder.BuildConnect(connection, "webPolling", null));
            }

            [Theory]
            [InlineData(null, "")]
            [InlineData("", "")]
            [InlineData("MyConnectionData", "&connectionData=MyConnectionData")]
            public void BuildConnectRetunsValidUrlWithConnectionData(string connectionData, string expected)
            {
                Assert.Equal(
                    "http://fakeurl/connect?clientProtocol=1.42&transport=webPolling" + expected + "&connectionToken=My%20Conn%20Token",
                    UrlBuilder.BuildConnect(CreateConnection(), "webPolling", connectionData));
            }

            [Theory]
            [InlineData("bob=12345", "&bob=12345")]
            [InlineData("bob=12345&foo=leet&baz=laskjdflsdk", "&bob=12345&foo=leet&baz=laskjdflsdk")]
            [InlineData("", "")]
            [InlineData(null, "")]
            [InlineData("?foo=bar", "&foo=bar")]
            [InlineData("?foo=bar&baz=bear", "&foo=bar&baz=bear")]
            [InlineData("&foo=bar", "&foo=bar")]
            [InlineData("&foo=bar&baz=bear", "&foo=bar&baz=bear")]
            public void BuildConnectRetunsValidUrlWithConnectionDataAndCustomQueryString(string qs, string expected)
            {
                Assert.Equal(
                    "http://fakeurl/connect?clientProtocol=1.42&transport=webPolling&connectionData=CustomConnectionData&connectionToken=My%20Conn%20Token" +
                    expected,
                    UrlBuilder.BuildConnect(CreateConnection(qs), "webPolling", "CustomConnectionData"));
            }
        }

        public class BuildReconnect
        {
            [Theory]
            [InlineData("42", "&messageId=42")]
            [InlineData("", "&messageId=")]
            [InlineData("4 2", "&messageId=4%202")]
            [InlineData(null, "")]
            public void BuildReconnectRetunsValidUrlWithMessageId(string messageId, string expected)
            {
                var connection = CreateConnection();
                Mock.Get(connection).Setup(c => c.MessageId).Returns(messageId);

                Assert.Equal(
                    "http://fakeurl/reconnect?clientProtocol=1.42&transport=webPolling&connectionToken=My%20Conn%20Token" +
                    expected,
                    UrlBuilder.BuildReconnect(connection, "webPolling", null));
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

                Assert.Equal(
                    "http://fakeurl/reconnect?clientProtocol=1.42&transport=webPolling&connectionToken=My%20Conn%20Token" +
                    expected,
                    UrlBuilder.BuildReconnect(connection, "webPolling", null));
            }

            [Theory]
            [InlineData(null, "")]
            [InlineData("", "")]
            [InlineData("MyConnectionData", "&connectionData=MyConnectionData")]
            public void BuildReconnectRetunsValidUrlWithConnectionData(string connectionData, string expected)
            {
                Assert.Equal(
                    "http://fakeurl/reconnect?clientProtocol=1.42&transport=webPolling"+ expected + "&connectionToken=My%20Conn%20Token",
                    UrlBuilder.BuildReconnect(CreateConnection(), "webPolling", connectionData));
            }

            [Theory]
            [InlineData("bob=12345", "&bob=12345")]
            [InlineData("bob=12345&foo=leet&baz=laskjdflsdk", "&bob=12345&foo=leet&baz=laskjdflsdk")]
            [InlineData("", "")]
            [InlineData(null, "")]
            [InlineData("?foo=bar", "&foo=bar")]
            [InlineData("?foo=bar&baz=bear", "&foo=bar&baz=bear")]
            [InlineData("&foo=bar", "&foo=bar")]
            [InlineData("&foo=bar&baz=bear", "&foo=bar&baz=bear")]
            public void BuildReconnectRetunsValidUrlWithConnectionDataAndCustomQueryString(string qs, string expected)
            {
                Assert.Equal(
                    "http://fakeurl/reconnect?clientProtocol=1.42&transport=webPolling&connectionData=CustomConnectionData&connectionToken=My%20Conn%20Token" +
                    expected,
                    UrlBuilder.BuildReconnect(CreateConnection(qs), "webPolling", "CustomConnectionData"));
            }
        }

        public class BuildPoll
        {
            [Theory]
            [InlineData("42", "&messageId=42")]
            [InlineData("", "&messageId=")]
            [InlineData("4 2", "&messageId=4%202")]
            [InlineData(null, "")]
            public void BuildPollRetunsValidUrlWithMessageId(string messageId, string expected)
            {
                var connection = CreateConnection();
                Mock.Get(connection).Setup(c => c.MessageId).Returns(messageId);

                Assert.Equal(
                    "http://fakeurl/poll?clientProtocol=1.42&transport=webPolling&connectionToken=My%20Conn%20Token" +
                    expected,
                    UrlBuilder.BuildPoll(connection, "webPolling", null));
            }

            [Theory]
            [InlineData("42", "&groupsToken=42")]
            [InlineData("", "&groupsToken=")]
            [InlineData("4 2", "&groupsToken=4%202")]
            [InlineData(null, "")]
            public void BuildPollRetunsValidUrlWithGroupsToken(string groupsToken, string expected)
            {
                var connection = CreateConnection();
                Mock.Get(connection).Setup(c => c.GroupsToken).Returns(groupsToken);

                Assert.Equal(
                    "http://fakeurl/poll?clientProtocol=1.42&transport=webPolling&connectionToken=My%20Conn%20Token" +
                    expected,
                    UrlBuilder.BuildPoll(connection, "webPolling", null));
            }

            [Theory]
            [InlineData(null, "")]
            [InlineData("", "")]
            [InlineData("MyConnectionData", "&connectionData=MyConnectionData")]
            public void BuildPollRetunsValidUrlWithConnectionData(string connectionData, string expected)
            {
                Assert.Equal(
                    "http://fakeurl/poll?clientProtocol=1.42&transport=webPolling" + expected + "&connectionToken=My%20Conn%20Token",
                    UrlBuilder.BuildPoll(CreateConnection(), "webPolling", connectionData));
            }

            [Theory]
            [InlineData("bob=12345", "&bob=12345")]
            [InlineData("bob=12345&foo=leet&baz=laskjdflsdk", "&bob=12345&foo=leet&baz=laskjdflsdk")]
            [InlineData("", "")]
            [InlineData(null, "")]
            [InlineData("?foo=bar", "&foo=bar")]
            [InlineData("?foo=bar&baz=bear", "&foo=bar&baz=bear")]
            [InlineData("&foo=bar", "&foo=bar")]
            [InlineData("&foo=bar&baz=bear", "&foo=bar&baz=bear")]
            public void BuildPollRetunsValidUrlWithConnectionDataAndCustomQueryString(string qs, string expected)
            {
                Assert.Equal(
                    "http://fakeurl/poll?clientProtocol=1.42&transport=webPolling&connectionData=CustomConnectionData&connectionToken=My%20Conn%20Token" +
                    expected,
                    UrlBuilder.BuildPoll(CreateConnection(qs), "webPolling", "CustomConnectionData"));
            }
        }

        public class BuildSend
        {
            [Theory]
            [InlineData(null, "")]
            [InlineData("", "")]
            [InlineData("MyConnectionData", "&connectionData=MyConnectionData")]
            public void BuildSendRetunsValidUrlWithConnectionData(string connectionData, string expected)
            {
                Assert.Equal(
                    "http://fakeurl/send?clientProtocol=1.42&transport=webPolling" + expected + "&connectionToken=My%20Conn%20Token",
                    UrlBuilder.BuildSend(CreateConnection(), "webPolling", connectionData));
            }

            [Theory]
            [InlineData("bob=12345", "&bob=12345")]
            [InlineData("bob=12345&foo=leet&baz=laskjdflsdk", "&bob=12345&foo=leet&baz=laskjdflsdk")]
            [InlineData("", "")]
            [InlineData(null, "")]
            [InlineData("?foo=bar", "&foo=bar")]
            [InlineData("?foo=bar&baz=bear", "&foo=bar&baz=bear")]
            [InlineData("&foo=bar", "&foo=bar")]
            [InlineData("&foo=bar&baz=bear", "&foo=bar&baz=bear")]
            public void BuildSendRetunsValidUrlWithConnectionDataAndCustomQueryString(string qs, string expected)
            {
                Assert.Equal(
                    "http://fakeurl/send?clientProtocol=1.42&transport=webPolling&connectionData=CustomConnectionData&connectionToken=My%20Conn%20Token" +
                    expected,
                    UrlBuilder.BuildSend(CreateConnection(qs), "webPolling", "CustomConnectionData"));
            }
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
