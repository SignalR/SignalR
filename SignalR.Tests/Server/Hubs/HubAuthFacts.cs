using System;
using System.Security.Principal;
using System.Threading;
using SignalR.Client.Hubs;
using SignalR.Hosting.Memory;
using SignalR.Hubs;
using Xunit;

namespace SignalR.Tests
{
    public class HubAuthFacts : IDisposable
    {
        [Fact]
        public void UnauthenticatedUserCanReceiveHubMessagesByDefault()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity(""), new string[] { });

            var hub = connection.CreateProxy("NoAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string, object>("joined", (id, time, authInfo) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void AuthenticatedUserCanReceiveHubMessagesByDefault()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("test"), new string[] { });

            var hub = connection.CreateProxy("NoAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string, object>("joined", (id, time, authInfo) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthenticatedUserCanInvokeMethodsByDefault()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity(""), new string[] { });

            var hub = connection.CreateProxy("NoAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            hub.Invoke("InvokedFromClient").Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void AuthenticatedUserCanInvokeMethodsByDefault()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("test"), new string[] { });

            var hub = connection.CreateProxy("NoAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            hub.Invoke("InvokedFromClient").Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthenticatedUserCannotReceiveHubMessagesWhenAuthenticationRequiredGlobally()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.HubPipeline.RequireAuthentication();
            host.User = new GenericPrincipal(new GenericIdentity(""), new string[] { });

            var hub = connection.CreateProxy("NoAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string, object>("joined", (id, time, authInfo) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void AuthenticatedUserCanReceiveHubMessagesWhenAuthenticationRequiredGlobally()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.HubPipeline.RequireAuthentication();
            host.User = new GenericPrincipal(new GenericIdentity("test"), new string[] { });

            var hub = connection.CreateProxy("NoAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string, object>("joined", (id, time, authInfo) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthenticatedUserCannotInvokeMethodsWhenAuthenticationRequiredGlobally()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.HubPipeline.RequireAuthentication();
            host.User = new GenericPrincipal(new GenericIdentity(""), new string[] { });

            var hub = connection.CreateProxy("NoAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.Throws<AggregateException>(() => hub.Invoke("InvokedFromClient").Wait());

            Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void AuthenticatedUserCanInvokeMethodsWhenAuthenticationRequiredGlobally()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.HubPipeline.RequireAuthentication();
            host.User = new GenericPrincipal(new GenericIdentity("test"), new string[] { });

            var hub = connection.CreateProxy("NoAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            hub.Invoke("InvokedFromClient").Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthenticatedUserCannotReceiveHubMessagesFromAuthorizedHubs()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity(""), new string[] { });

            var hub = connection.CreateProxy("AuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string, object>("joined", (id, time, authInfo) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void AuthenticatedUserCanReceiveHubMessagesFromAuthorizedHubs()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("test"), new string[] { });

            var hub = connection.CreateProxy("AuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string, object>("joined", (id, time, authInfo) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthenticatedUserCannotInvokeMethodsInAuthorizedHubs()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity(""), new string[] { });

            var hub = connection.CreateProxy("AuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.Throws<AggregateException>(() => hub.Invoke("InvokedFromClient").Wait());

            Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void AuthenticatedUserCanInvokeMethodsInAuthorizedHubs()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("test"), new string[] { });

            var hub = connection.CreateProxy("AuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            hub.Invoke("InvokedFromClient").Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthenticatedUserCannotReceiveHubMessagesFromHubsInheritingFromAuthorizedHubs()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity(""), new string[] { });

            var hub = connection.CreateProxy("InheritAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string, object>("joined", (id, time, authInfo) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void AuthenticatedUserCanReceiveHubMessagesFromHubsInheritingFromAuthorizedHubs()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("test"), new string[] { });

            var hub = connection.CreateProxy("InheritAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string, object>("joined", (id, time, authInfo) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthenticatedUserCannotInvokeMethodsInHubsInheritingFromAuthorizedHubs()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity(""), new string[] { });

            var hub = connection.CreateProxy("InheritAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.Throws<AggregateException>(() => hub.Invoke("InvokedFromClient").Wait());

            Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void AuthenticatedUserCanInvokeMethodsInHubsInheritingFromAuthorizedHubs()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("test"), new string[] { });

            var hub = connection.CreateProxy("InheritAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            hub.Invoke("InvokedFromClient").Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthenticatedUserCannotReceiveHubMessagesFromHubsAuthorizedWithRoles()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity(""), new string[] { "Admin" });

            var hub = connection.CreateProxy("AdminAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string, object>("joined", (id, time, authInfo) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthenticatedUserCannotInvokeMethodsInHubsAuthorizedWithRoles()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity(""), new string[] { "Admin" });

            var hub = connection.CreateProxy("AdminAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.Throws<AggregateException>(() => hub.Invoke("InvokedFromClient").Wait());

            Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthorizedUserCannotReceiveHubMessagesFromHubsAuthorizedWithRoles()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("test"), new string[] { "User", "NotAdmin" });

            var hub = connection.CreateProxy("AdminAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string, object>("joined", (id, time, authInfo) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthorizedUserCannotInvokeMethodsInHubsAuthorizedWithRoles()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("test"), new string[] { "User", "NotAdmin" });

            var hub = connection.CreateProxy("AdminAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.Throws<AggregateException>(() => hub.Invoke("InvokedFromClient").Wait());

            Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void AuthenticatedAndAuthorizedUserCanReceiveHubMessagesFromHubsAuthorizedWithRoles()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("test"), new string[] { "User", "Admin" });

            var hub = connection.CreateProxy("AdminAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string, object>("joined", (id, time, authInfo) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void AuthenticatedAndAuthorizedUserCanInvokeMethodsInHubsAuthorizedWithRoles()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("test"), new string[] { "Admin" });

            var hub = connection.CreateProxy("AdminAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            hub.Invoke("InvokedFromClient").Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthorizedUserCannotReceiveHubMessagesFromHubsAuthorizedSpecifyingUserAndRole()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("User"), new string[] { "User", "NotAdmin" });

            var hub = connection.CreateProxy("UserAndRoleAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string, object>("joined", (id, time, authInfo) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthorizedUserCannotInvokeMethodsInHubsAuthorizedSpecifyingUserAndRole()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("test"), new string[] { "User", "Admin" });

            var hub = connection.CreateProxy("UserAndRoleAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.Throws<AggregateException>(() => hub.Invoke("InvokedFromClient").Wait());

            Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void AuthenticatedAndAuthorizedUserCanReceiveHubMessagesFromHubsAuthorizedSpecifyingUserAndRole()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("User"), new string[] { "test", "Admin" });

            var hub = connection.CreateProxy("UserAndRoleAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string, object>("joined", (id, time, authInfo) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void AuthenticatedAndAuthorizedUserCanInvokeMethodsInHubsAuthorizedSpecifyingUserAndRole()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("User"), new string[] { "Admin" });

            var hub = connection.CreateProxy("UserAndRoleAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            hub.Invoke("InvokedFromClient").Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthenticatedUserCanReceiveHubMessagesFromIncomingAuthorizedHubs()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity(""), new string[] { "User", "NotAdmin" });

            var hub = connection.CreateProxy("IncomingAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string, object>("joined", (id, time, authInfo) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthenticatedUserCannotInvokeMethodsInIncomingAuthorizedHubs()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity(""), new string[] { "User", "NotAdmin" });

            var hub = connection.CreateProxy("IncomingAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.Throws<AggregateException>(() => hub.Invoke("InvokedFromClient").Wait());

            Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void AuthenticatedUserCanReceiveHubMessagesFromIncomingAuthorizedHubs()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("Admin"), new string[] { });

            var hub = connection.CreateProxy("IncomingAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string, object>("joined", (id, time, authInfo) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void AuthenticatedUserCanInvokeMethodsInIncomingAuthorizedHubs()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("User"), new string[] { "Admin" });

            var hub = connection.CreateProxy("IncomingAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            hub.Invoke("InvokedFromClient").Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthenticatedUserCannotReceiveHubMessagesFromOutgoingAuthorizedHubs()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity(""), new string[] { "User", "NotAdmin" });

            var hub = connection.CreateProxy("OutgoingAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string, object>("joined", (id, time, authInfo) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthenticatedUserCanInvokeMethodsInOutgoingAuthorizedHubs()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity(""), new string[] { "User", "NotAdmin" });

            var hub = connection.CreateProxy("OutgoingAuthHub");

            connection.Start(host).Wait();

            hub.Invoke("InvokedFromClient").Wait();

            connection.Stop();
        }

        [Fact]
        public void AuthenticatedUserCanReceiveHubMessagesFromOutgoingAuthorizedHubs()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("Admin"), new string[] { });

            var hub = connection.CreateProxy("OutgoingAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string, object>("joined", (id, time, authInfo) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void AuthenticatedUserCanInvokeMethodsInOutgoingAuthorizedHubs()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("User"), new string[] { "Admin" });

            var hub = connection.CreateProxy("OutgoingAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            hub.Invoke("InvokedFromClient").Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthenticatedUserCannotInvokeAuthorizedHubMethods()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity(""), new string[] { "Admin", "Invoker" });

            var hub = connection.CreateProxy("InvokeAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.Throws<AggregateException>(() => hub.Invoke("InvokedFromClient").Wait());

            Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void UnauthorizedUserCannotInvokeAuthorizedHubMethods()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("test"), new string[] { "User", "NotAdmin" });

            var hub = connection.CreateProxy("InvokeAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            Assert.Throws<AggregateException>(() => hub.Invoke("InvokedFromClient").Wait());

            Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        [Fact]
        public void AuthenticatedAndAuthorizedUserCanInvokeAuthorizedHubMethods()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            host.User = new GenericPrincipal(new GenericIdentity("test"), new string[] { "User", "Admin" });

            var hub = connection.CreateProxy("InvokeAuthHub");
            var wh = new ManualResetEvent(false);
            hub.On<string, string>("invoked", (id, time) =>
            {
                Assert.NotNull(id);
                wh.Set();
            });

            connection.Start(host).Wait();

            hub.Invoke("InvokedFromClient").Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
            connection.Stop();
        }

        public void Dispose()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
