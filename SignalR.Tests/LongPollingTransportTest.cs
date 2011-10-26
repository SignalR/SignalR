using System.Collections.Specialized;
using System.Web;
using Moq;
using SignalR.Transports;
using Xunit;

namespace SignalR.Tests
{
    public class LongPollingTransportTest
    {
        [Fact]
        public void ExtractsClientIdFromRequest()
        {
            var context = new Mock<HttpContextBase>();
            var request = new Mock<HttpRequestBase>();
            request.Setup(m => m["clientId"]).Returns("1");
            context.Setup(m => m.Request).Returns(request.Object);
            var json = new Mock<IJsonStringifier>();
            var heartBeat = new Mock<ITransportHeartBeat>();
            var transport = new LongPollingTransport(context.Object, json.Object, heartBeat.Object);

            Assert.Equal("1", transport.ClientId);
        }

        [Fact]
        public void IsAliveReturnsTrueIfClientIsConnected()
        {
            var context = new Mock<HttpContextBase>();
            var response = new Mock<HttpResponseBase>();
            response.Setup(m => m.IsClientConnected).Returns(true);
            context.Setup(m => m.Response).Returns(response.Object);
            var json = new Mock<IJsonStringifier>();
            var heartBeat = new Mock<ITransportHeartBeat>();
            var transport = new LongPollingTransport(context.Object, json.Object, heartBeat.Object);

            Assert.True(transport.IsAlive);
        }

        [Fact]
        public void DisconnectRaisedDisconnectEvent()
        {
            var context = new Mock<HttpContextBase>();
            var response = new Mock<HttpResponseBase>();
            response.Setup(m => m.IsClientConnected).Returns(false);
            context.Setup(m => m.Response).Returns(response.Object);
            var json = new Mock<IJsonStringifier>();
            var heartBeat = new Mock<ITransportHeartBeat>();
            var transport = new LongPollingTransport(context.Object, json.Object, heartBeat.Object);

            var eventRaised = false;
            transport.Disconnected += () =>
            {
                eventRaised = true;
            };

            transport.Disconnect();
            Assert.True(eventRaised);
        }

        [Fact]
        public void ConnectRequestAddsConnectionToHeartBeat()
        {
            var context = new Mock<HttpContextBase>();
            var request = new Mock<HttpRequestBase>();
            request.Setup(m => m.Path).Returns("/foo/connect");
            request.Setup(m => m["clientId"]).Returns("1");
            context.Setup(m => m.Request).Returns(request.Object);
            var json = new Mock<IJsonStringifier>();
            var heartBeat = new Mock<ITransportHeartBeat>();
            var transport = new LongPollingTransport(context.Object, json.Object, heartBeat.Object);
            var connection = new Mock<IConnection>();

            transport.ProcessRequest(connection.Object);
            heartBeat.Verify(m => m.AddConnection(transport), Times.Once());
        }

        [Fact]
        public void ConnectRequestRaisesConnectEvent()
        {
            var context = new Mock<HttpContextBase>();
            var request = new Mock<HttpRequestBase>();
            request.Setup(m => m.Path).Returns("/foo/connect");
            request.Setup(m => m["clientId"]).Returns("1");
            context.Setup(m => m.Request).Returns(request.Object);
            var json = new Mock<IJsonStringifier>();
            var heartBeat = new Mock<ITransportHeartBeat>();
            var transport = new LongPollingTransport(context.Object, json.Object, heartBeat.Object);
            var connection = new Mock<IConnection>();
            bool eventRaised = false;

            transport.Connected += () =>
            {
                eventRaised = true;
            };

            transport.ProcessRequest(connection.Object);
            Assert.True(eventRaised);
        }

        [Fact]
        public void ConnectRequestCallsReceiveOnConnectionThenSend()
        {
            var context = new Mock<HttpContextBase>();
            var request = new Mock<HttpRequestBase>();
            var response = new Mock<HttpResponseBase>();
            request.Setup(m => m.Path).Returns("/foo/connect");
            request.Setup(m => m["clientId"]).Returns("1");
            context.Setup(m => m.Response).Returns(response.Object);
            context.Setup(m => m.Request).Returns(request.Object);
            var json = new Mock<IJsonStringifier>();
            var heartBeat = new Mock<ITransportHeartBeat>();
            var transport = new Mock<LongPollingTransport>(context.Object, json.Object, heartBeat.Object) { CallBase = true };
            var connection = new Mock<IConnection>();
            PersistentResponse persistentResponse = null;
            connection.Setup(m => m.ReceiveAsync()).Returns(TaskAsyncHelper.FromResult<PersistentResponse>(persistentResponse));

            transport.Object.ProcessRequest(connection.Object)().Wait();

            heartBeat.Verify(m => m.AddConnection(transport.Object), Times.Once());
            heartBeat.Verify(m => m.RemoveConnection(transport.Object), Times.Once());
            connection.Verify(m => m.ReceiveAsync(), Times.Once());
            transport.Verify(m => m.Send(persistentResponse), Times.Once());
            response.VerifySet(m => m.ContentType = "application/json");
            response.Verify(m => m.Write(It.IsAny<string>()), Times.Once());
            json.Verify(m => m.Stringify(persistentResponse), Times.Once());
        }

        [Fact]
        public void RequestWithMessageIdCallsReceiveOnConnectionThenSend()
        {
            var context = new Mock<HttpContextBase>();
            var request = new Mock<HttpRequestBase>();
            var response = new Mock<HttpResponseBase>();
            request.Setup(m => m.Path).Returns("/foo");
            request.Setup(m => m["clientId"]).Returns("1");
            request.Setup(m => m["messageId"]).Returns("20");
            context.Setup(m => m.Response).Returns(response.Object);
            context.Setup(m => m.Request).Returns(request.Object);
            var json = new Mock<IJsonStringifier>();
            var heartBeat = new Mock<ITransportHeartBeat>();
            var transport = new Mock<LongPollingTransport>(context.Object, json.Object, heartBeat.Object) { CallBase = true };
            var connection = new Mock<IConnection>();
            PersistentResponse persistentResponse = null;
            connection.Setup(m => m.ReceiveAsync(20)).Returns(TaskAsyncHelper.FromResult<PersistentResponse>(persistentResponse));

            transport.Object.ProcessRequest(connection.Object)().Wait();

            heartBeat.Verify(m => m.AddConnection(transport.Object), Times.Once());
            heartBeat.Verify(m => m.RemoveConnection(transport.Object), Times.Once());
            connection.Verify(m => m.ReceiveAsync(20), Times.Once());
            transport.Verify(m => m.Send(persistentResponse), Times.Once());
            response.VerifySet(m => m.ContentType = "application/json");
            response.Verify(m => m.Write(It.IsAny<string>()), Times.Once());
            json.Verify(m => m.Stringify(persistentResponse), Times.Once());
        }

        [Fact]
        public void ProcessRequestReturnsNullIfRequestWithMalformedMessageId()
        {
            var context = new Mock<HttpContextBase>();
            var request = new Mock<HttpRequestBase>();
            var response = new Mock<HttpResponseBase>();
            request.Setup(m => m.Path).Returns("/foo");
            request.Setup(m => m["clientId"]).Returns("1");
            request.Setup(m => m["messageId"]).Returns("fff");
            context.Setup(m => m.Response).Returns(response.Object);
            context.Setup(m => m.Request).Returns(request.Object);
            var json = new Mock<IJsonStringifier>();
            var heartBeat = new Mock<ITransportHeartBeat>();
            var transport = new LongPollingTransport(context.Object, json.Object, heartBeat.Object);
            var connection = new Mock<IConnection>();

            var func = transport.ProcessRequest(connection.Object);

            Assert.Null(func);
        }

        [Fact]
        public void SendSetsContentTypeAndWritesSerializedResponse()
        {
            var obj = new { A = 1 };
            var context = new Mock<HttpContextBase>();
            var request = new Mock<HttpRequestBase>();
            var response = new Mock<HttpResponseBase>();
            request.Setup(m => m.Path).Returns("/foo/");
            request.Setup(m => m["clientId"]).Returns("1");
            context.Setup(m => m.Response).Returns(response.Object);
            context.Setup(m => m.Request).Returns(request.Object);
            var json = new Mock<IJsonStringifier>();
            json.Setup(m => m.Stringify(obj)).Returns("A=1");
            var heartBeat = new Mock<ITransportHeartBeat>();
            var transport = new LongPollingTransport(context.Object, json.Object, heartBeat.Object);
            var connection = new Mock<IConnection>();
            transport.Send(obj);

            response.VerifySet(m => m.ContentType = "application/json");
            response.Verify(m => m.Write("A=1"), Times.Once());
        }

        [Fact]
        public void SendRequestRaisesOnReceived()
        {
            var context = new Mock<HttpContextBase>();
            var request = new Mock<HttpRequestBase>();
            request.Setup(m => m.Path).Returns("/foo/send");
            var form = new NameValueCollection();
            form["data"] = "some data";
            request.Setup(m => m.Form).Returns(form);
            context.Setup(m => m.Request).Returns(request.Object);
            var json = new Mock<IJsonStringifier>();
            var heartBeat = new Mock<ITransportHeartBeat>();
            var transport = new LongPollingTransport(context.Object, json.Object, heartBeat.Object);
            var connection = new Mock<IConnection>();
            bool eventRaised = false;

            transport.Received += data =>
            {
                eventRaised = true;
                Assert.Equal(data, "some data");
            };

            transport.ProcessRequest(connection.Object);
            Assert.True(eventRaised);
        }
    }
}
