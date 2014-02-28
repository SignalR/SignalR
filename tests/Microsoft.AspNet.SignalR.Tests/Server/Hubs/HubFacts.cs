using System;
using System.Dynamic;
using Microsoft.AspNet.SignalR.Hubs;
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
            var mockClients = new Mock<IHubCallerConnectionContext>();

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
            var mockClients = new Mock<IHubCallerConnectionContext>();
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
            var mockClients = new Mock<IHubCallerConnectionContext>();
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
            var mockClients = new Mock<IHubCallerConnectionContext>();
            var clients = new Mock<IClientContract>();

            hub.Clients = mockClients.Object;
            clients.Setup(m => m.send(It.IsAny<string>())).Verifiable();
            mockClients.Setup(m => m.Client("random")).Returns(clients.Object);
            hub.SendIndividual("random", "foo");

            clients.VerifyAll();
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
        }

        public interface IClientContract
        {
            void send(string messages);
        }
    }
}
