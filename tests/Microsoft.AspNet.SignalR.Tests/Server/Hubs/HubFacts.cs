using System;
using System.Collections.Specialized;
using System.Dynamic;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server.Hubs
{
    public class HubFacts
    {
        [Fact]
        public void HubsAreMockableViaDynamic()
        {
            bool sendCalled = false;
            var hub = new MyTestableHub();
            var mockClients = new Mock<IHubCallerConnectionContext<dynamic>>();

            hub.Clients = mockClients.Object;

            dynamic all = new ExpandoObject();
            all.send = new Action<string>(message =>
            {
                sendCalled = true;
            });

            mockClients.Setup(m => m.All).Returns((ExpandoObject)all);
            hub.Send("foo");

            Assert.True(sendCalled);
        }

        [Fact]
        public void HubsAreMockableViaType()
        {
            var hub = new MyTestableHub();
            var mockClients = new Mock<IHubCallerConnectionContext<dynamic>>();
            var all = new Mock<IClientContract>();

            hub.Clients = mockClients.Object;
            all.Setup(m => m.send(It.IsAny<string>())).Verifiable();
            mockClients.Setup(m => m.All).Returns(all.Object);
            hub.Send("foo");

            all.VerifyAll();
        }

        [Fact]
        public void HubsGroupAreMockable()
        {
            var hub = new MyTestableHub();
            var mockClients = new Mock<IHubCallerConnectionContext<dynamic>>();
            var groups = new Mock<IClientContract>();

            hub.Clients = mockClients.Object;
            groups.Setup(m => m.send(It.IsAny<string>())).Verifiable();
            mockClients.Setup(m => m.Group("test")).Returns(groups.Object);
            hub.SendGroup("test", "foo");

            groups.VerifyAll();
        }

        [Fact]
        public void HubsClientIsMockable()
        {
            var hub = new MyTestableHub();
            var mockClients = new Mock<IHubCallerConnectionContext<dynamic>>();
            var clients = new Mock<IClientContract>();

            hub.Clients = mockClients.Object;
            clients.Setup(m => m.send(It.IsAny<string>())).Verifiable();
            mockClients.Setup(m => m.Client("random")).Returns(clients.Object);
            hub.SendIndividual("random", "foo");

            clients.VerifyAll();
        }

        [Fact]
        public void HubCallerContextIsMockable()
        {
            var hub = new MyTestableHub();
            var context = new Mock<HubCallerContext>();

            var mockClients = new Mock<IHubCallerConnectionContext<dynamic>>();
            var clients = new Mock<IClientContract>();

            hub.Clients = mockClients.Object;
            clients.Setup(m => m.send(It.IsAny<string>())).Verifiable();
            mockClients.Setup(m => m.Client("1")).Returns(clients.Object);

            hub.Context = context.Object;

            var qs = new NameValueCollection();
            qs["connectionId"] = "1";
            context.Setup(c => c.QueryString).Returns(new NameValueCollectionWrapper(qs));

            hub.SendToOneClient();

            clients.VerifyAll();
        }

        [Fact]
        public void HubsCanExplicitelyImplementIHub()
        {
            // https://github.com/SignalR/SignalR/issues/3228
            var mockClients = new Mock<IHubCallerConnectionContext<dynamic>>();
            var all = new Mock<IClientContract>();
            all.Setup(m => m.send("foo"));
            mockClients.Setup(m => m.All).Returns(all.Object);

            var hub = new MyIHub();
            hub.Clients = mockClients.Object;
            hub.Send("foo");

            mockClients.VerifyAll();
            all.VerifyAll();
        }

        [Fact]
        public void TypedHubsCanExplicitelyImplementIHub()
        {
            // https://github.com/SignalR/SignalR/issues/3228
            var mockClients = new Mock<IHubCallerConnectionContext<dynamic>>();
            var all = new Mock<IClientProxy>();
            all.Setup(m => m.Invoke("send", "foo"));
            mockClients.Setup(m => m.All).Returns(all.Object);

            var typedHub = new MyTypedIHub();
            ((IHub)typedHub).Clients = mockClients.Object;
            typedHub.Send("foo");

            mockClients.VerifyAll();
            all.VerifyAll();
        }

        [Fact]
        public void TypedIHubCallerConnectionContextIsSettable()
        {
            // https://github.com/SignalR/SignalR/issues/3299
            var mockClients = new Mock<IHubCallerConnectionContext<IClientContract>>();
            var all = new Mock<IClientContract>();
            all.Setup(m => m.send("foo"));
            mockClients.Setup(m => m.All).Returns(all.Object);

            var hub = new MyTypedIHub();
            hub.Clients = mockClients.Object;
            hub.Send("foo");

            mockClients.VerifyAll();
            all.VerifyAll();
        }

        private class MyTestableHub : Hub
        {
            public void Send(string messages)
            {
                Clients.All.send(messages);
            }

            public void SendGroup(string group, string message)
            {
                Clients.Group(group).send(message);
            }

            public void SendIndividual(string connectionId, string message)
            {
                Clients.Client(connectionId).send(message);
            }

            public void SendToOneClient()
            {
                string connectionId = Context.QueryString["connectionId"];
                Clients.Client(connectionId).send("foo");
            }

        }

        private class MyIHub : Hub, IHub
        {
            public void Send(string messages)
            {
                Clients.All.send(messages);
            }
        }

        private class MyTypedIHub : Hub<IClientContract>, IHub
        {
            public void Send(string messages)
            {
                Clients.All.send(messages);
            }
        }

        public interface IClientContract
        {
            void send(string messages);
        }
    }
}
