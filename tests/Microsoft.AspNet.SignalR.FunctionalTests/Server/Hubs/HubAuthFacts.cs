using System;
using System.Security.Principal;
using System.Threading;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.FunctionalTests;
using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Hubs;
using Owin;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    using AppFunc = Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

    public class HubAuthFacts : HostedTest
    {
        [Fact]
        public void UnauthenticatedUserCanReceiveHubMessagesByDefault()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity(""), new string[] { }));
                    app.MapHubs("/signalr", configuration);

                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("NoAuthHub");
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
        }

        [Fact]
        public void AuthenticatedUserCanReceiveHubMessagesByDefault()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity("test"), new string[] { }));
                    app.MapHubs("/signalr", configuration);

                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("NoAuthHub");
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
        }

        [Fact]
        public void UnauthenticatedUserCanInvokeMethodsByDefault()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity(""), new string[] { }));
                    app.MapHubs("/signalr", configuration);

                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("NoAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string>("invoked", (id, time) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                connection.Start(host).Wait();

                hub.InvokeWithTimeout("InvokedFromClient");

                Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                connection.Stop();
            }
        }

        [Fact]
        public void AuthenticatedUserCanInvokeMethodsByDefault()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity("test"), new string[] { }));
                    app.MapHubs("/signalr", configuration);

                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("NoAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string>("invoked", (id, time) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                connection.Start(host).Wait();

                hub.InvokeWithTimeout("InvokedFromClient");

                Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                connection.Stop();
            }
        }

        [Fact]
        public void UnauthenticatedUserCannotReceiveHubMessagesWhenAuthenticationRequiredGlobally()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    configuration.Resolver.Resolve<IHubPipeline>().RequireAuthentication();

                    WithUser(app, new GenericPrincipal(new GenericIdentity(""), new string[] { }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");                

                var hub = connection.CreateHubProxy("NoAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string, object>("joined", (id, time, authInfo) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                Assert.Throws<AggregateException>(() => connection.Start(host).Wait());

                connection.Stop();
            }
        }

        [Fact]
        public void AuthenticatedUserCanReceiveHubMessagesWhenAuthenticationRequiredGlobally()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    configuration.Resolver.Resolve<IHubPipeline>().RequireAuthentication();

                    WithUser(app, new GenericPrincipal(new GenericIdentity("test"), new string[] { }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("NoAuthHub");
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
        }

        [Fact]
        public void UnauthenticatedUserCannotInvokeMethodsWhenAuthenticationRequiredGlobally()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    configuration.Resolver.Resolve<IHubPipeline>().RequireAuthentication();

                    WithUser(app, new GenericPrincipal(new GenericIdentity(""), new string[] { }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("NoAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string>("invoked", (id, time) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                Assert.Throws<AggregateException>(() => connection.Start(host).Wait());

                connection.Stop();
            }
        }

        [Fact]
        public void AuthenticatedUserCanInvokeMethodsWhenAuthenticationRequiredGlobally()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    configuration.Resolver.Resolve<IHubPipeline>().RequireAuthentication();

                    WithUser(app, new GenericPrincipal(new GenericIdentity("test"), new string[] { }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("NoAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string>("invoked", (id, time) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                connection.Start(host).Wait();

                hub.InvokeWithTimeout("InvokedFromClient");

                Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                connection.Stop();
            }
        }

        [Fact]
        public void UnauthenticatedUserCannotReceiveHubMessagesFromAuthorizedHubs()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity(""), new string[] { }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("AuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string, object>("joined", (id, time, authInfo) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                Assert.Throws<AggregateException>(() => connection.Start(host).Wait());

                connection.Stop();
            }
        }

        [Fact]
        public void AuthenticatedUserCanReceiveHubMessagesFromAuthorizedHubs()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity("test"), new string[] { }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("AuthHub");
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
        }

        [Fact]
        public void UnauthenticatedUserCannotInvokeMethodsInAuthorizedHubs()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity(""), new string[] { }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("AuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string>("invoked", (id, time) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                Assert.Throws<AggregateException>(() => connection.Start(host).Wait());

                connection.Stop();
            }
        }

        [Fact]
        public void AuthenticatedUserCanInvokeMethodsInAuthorizedHubs()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity("test"), new string[] { }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("AuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string>("invoked", (id, time) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                connection.Start(host).Wait();

                hub.InvokeWithTimeout("InvokedFromClient");

                Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                connection.Stop();
            }
        }

        [Fact]
        public void UnauthenticatedUserCannotReceiveHubMessagesFromHubsInheritingFromAuthorizedHubs()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity(""), new string[] { }));
                    app.MapHubs("/signalr", configuration);
                });
                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("InheritAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string, object>("joined", (id, time, authInfo) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                Assert.Throws<AggregateException>(() => connection.Start(host).Wait());

                connection.Stop();
            }
        }

        [Fact]
        public void AuthenticatedUserCanReceiveHubMessagesFromHubsInheritingFromAuthorizedHubs()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity("test"), new string[] { }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("InheritAuthHub");
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
        }

        [Fact]
        public void UnauthenticatedUserCannotInvokeMethodsInHubsInheritingFromAuthorizedHubs()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity(""), new string[] { }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("InheritAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string>("invoked", (id, time) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                Assert.Throws<AggregateException>(() => connection.Start(host).Wait());

                connection.Stop();
            }
        }

        [Fact]
        public void AuthenticatedUserCanInvokeMethodsInHubsInheritingFromAuthorizedHubs()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity("test"), new string[] { }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("InheritAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string>("invoked", (id, time) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                connection.Start(host).Wait();

                hub.InvokeWithTimeout("InvokedFromClient");

                Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                connection.Stop();
            }
        }

        [Fact]
        public void UnauthenticatedUserCannotReceiveHubMessagesFromHubsAuthorizedWithRoles()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity(""), new string[] { "Admin" }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("AdminAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string, object>("joined", (id, time, authInfo) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                Assert.Throws<AggregateException>(() => connection.Start(host).Wait());

                connection.Stop();
            }
        }

        [Fact]
        public void UnauthenticatedUserCannotInvokeMethodsInHubsAuthorizedWithRoles()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity(""), new string[] { "Admin" }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("AdminAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string>("invoked", (id, time) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                Assert.Throws<AggregateException>(() => connection.Start(host).Wait());
                
                connection.Stop();
            }
        }

        [Fact]
        public void UnauthorizedUserCannotReceiveHubMessagesFromHubsAuthorizedWithRoles()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity("test"), new string[] { "User", "NotAdmin" }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("AdminAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string, object>("joined", (id, time, authInfo) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                Assert.Throws<AggregateException>(() => connection.Start(host).Wait());

                connection.Stop();
            }
        }

        [Fact]
        public void UnauthorizedUserCannotInvokeMethodsInHubsAuthorizedWithRoles()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity("test"), new string[] { "User", "NotAdmin" }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("AdminAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string>("invoked", (id, time) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                Assert.Throws<AggregateException>(() => connection.Start(host).Wait());

                connection.Stop();
            }
        }

        [Fact]
        public void AuthenticatedAndAuthorizedUserCanReceiveHubMessagesFromHubsAuthorizedWithRoles()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity("test"), new string[] { "User", "Admin" }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("AdminAuthHub");
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
        }

        [Fact]
        public void AuthenticatedAndAuthorizedUserCanInvokeMethodsInHubsAuthorizedWithRoles()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity("test"), new string[] { "Admin" }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("AdminAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string>("invoked", (id, time) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                connection.Start(host).Wait();

                hub.InvokeWithTimeout("InvokedFromClient");

                Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                connection.Stop();
            }
        }

        [Fact]
        public void UnauthorizedUserCannotReceiveHubMessagesFromHubsAuthorizedSpecifyingUserAndRole()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity("User"), new string[] { "User", "NotAdmin" }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("UserAndRoleAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string, object>("joined", (id, time, authInfo) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                Assert.Throws<AggregateException>(() => connection.Start(host).Wait());

                connection.Stop();
            }
        }

        [Fact]
        public void UnauthorizedUserCannotInvokeMethodsInHubsAuthorizedSpecifyingUserAndRole()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity("test"), new string[] { "User", "Admin" }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("UserAndRoleAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string>("invoked", (id, time) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                Assert.Throws<AggregateException>(() => connection.Start(host).Wait());

                connection.Stop();
            }
        }

        [Fact]
        public void AuthenticatedAndAuthorizedUserCanReceiveHubMessagesFromHubsAuthorizedSpecifyingUserAndRole()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity("User"), new string[] { "test", "Admin" }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("UserAndRoleAuthHub");
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
        }

        [Fact]
        public void AuthenticatedAndAuthorizedUserCanInvokeMethodsInHubsAuthorizedSpecifyingUserAndRole()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity("User"), new string[] { "Admin" }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("UserAndRoleAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string>("invoked", (id, time) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                connection.Start(host).Wait();

                hub.InvokeWithTimeout("InvokedFromClient");

                Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                connection.Stop();
            }
        }

        [Fact]
        public void UnauthenticatedUserCanReceiveHubMessagesFromIncomingAuthorizedHubs()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity(""), new string[] { "User", "NotAdmin" }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("IncomingAuthHub");
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
        }

        [Fact]
        public void UnauthenticatedUserCannotInvokeMethodsInIncomingAuthorizedHubs()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity(""), new string[] { "User", "NotAdmin" }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("IncomingAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string>("invoked", (id, time) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                connection.Start(host).Wait();

                Assert.Throws<AggregateException>(() => hub.InvokeWithTimeout("InvokedFromClient"));

                Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
                connection.Stop();
            }
        }

        [Fact]
        public void AuthenticatedUserCanReceiveHubMessagesFromIncomingAuthorizedHubs()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity("Admin"), new string[] { }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("IncomingAuthHub");
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
        }

        [Fact]
        public void AuthenticatedUserCanInvokeMethodsInIncomingAuthorizedHubs()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity("User"), new string[] { "Admin" }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("IncomingAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string>("invoked", (id, time) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                connection.Start(host).Wait();

                hub.InvokeWithTimeout("InvokedFromClient");

                Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                connection.Stop();
            }
        }

        [Fact]
        public void UnauthenticatedUserCannotInvokeAuthorizedHubMethods()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity(""), new string[] { "Admin", "Invoker" }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("InvokeAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string>("invoked", (id, time) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                connection.Start(host).Wait();

                Assert.Throws<AggregateException>(() => hub.InvokeWithTimeout("InvokedFromClient"));

                Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
                connection.Stop();
            }
        }

        [Fact]
        public void UnauthorizedUserCannotInvokeAuthorizedHubMethods()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity("test"), new string[] { "User", "NotAdmin" }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("InvokeAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string>("invoked", (id, time) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                connection.Start(host).Wait();

                Assert.Throws<AggregateException>(() => hub.InvokeWithTimeout("InvokedFromClient"));

                Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
                connection.Stop();
            }
        }

        [Fact]
        public void AuthenticatedAndAuthorizedUserCanInvokeAuthorizedHubMethods()
        {
            using (var host = new MemoryHost())
            {
                host.Configure(app =>
                {
                    var configuration = new HubConfiguration
                    {
                        Resolver = new DefaultDependencyResolver()
                    };

                    WithUser(app, new GenericPrincipal(new GenericIdentity("test"), new string[] { "User", "Admin" }));
                    app.MapHubs("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                var hub = connection.CreateHubProxy("InvokeAuthHub");
                var wh = new ManualResetEvent(false);
                hub.On<string, string>("invoked", (id, time) =>
                {
                    Assert.NotNull(id);
                    wh.Set();
                });

                connection.Start(host).Wait();

                hub.InvokeWithTimeout("InvokedFromClient");

                Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                connection.Stop();
            }
        }

        private static void WithUser(IAppBuilder app, IPrincipal user)
        {
            Func<AppFunc, AppFunc> middleware = (next) =>
            {
                return env =>
                {
                    env["server.User"] = user;
                    return next(env);
                };
            };

            app.Use(middleware);
        }
    }
}
