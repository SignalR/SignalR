using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Tests.Common;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Owin;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class HubAuthFacts : HostedTest
    {
        [Fact]
        public async Task UnauthenticatedUserCanReceiveHubMessagesByDefault()
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
                    app.MapSignalR("/signalr", configuration);

                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("NoAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string, object>("joined", (id, time, authInfo) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
            }
        }

        [Fact]
        public async Task AuthenticatedUserCanReceiveHubMessagesByDefault()
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
                    app.MapSignalR("/signalr", configuration);

                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("NoAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string, object>("joined", (id, time, authInfo) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
            }
        }

        [Fact]
        public async Task UnauthenticatedUserCanInvokeMethodsByDefault()
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
                    app.MapSignalR("/signalr", configuration);

                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("NoAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string>("invoked", (id, time) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    hub.InvokeWithTimeout("InvokedFromClient");

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
            }
        }

        [Fact]
        public async Task AuthenticatedUserCanInvokeMethodsByDefault()
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("NoAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string>("invoked", (id, time) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    hub.InvokeWithTimeout("InvokedFromClient");

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("NoAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string, object>("joined", (id, time, authInfo) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    Assert.Throws<AggregateException>(() => connection.Start(host).Wait());
                }
            }
        }

        [Fact]
        public async Task AuthenticatedUserCanReceiveHubMessagesWhenAuthenticationRequiredGlobally()
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("NoAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string, object>("joined", (id, time, authInfo) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("NoAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string>("invoked", (id, time) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    Assert.Throws<AggregateException>(() => connection.Start(host).Wait());
                }
            }
        }

        [Fact]
        public async Task AuthenticatedUserCanInvokeMethodsWhenAuthenticationRequiredGlobally()
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("NoAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string>("invoked", (id, time) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    hub.InvokeWithTimeout("InvokedFromClient");

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("AuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string, object>("joined", (id, time, authInfo) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    Assert.Throws<AggregateException>(() => connection.Start(host).Wait());
                }
            }
        }

        [Fact]
        public async Task AuthenticatedUserCanReceiveHubMessagesFromAuthorizedHubs()
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("AuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string, object>("joined", (id, time, authInfo) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("AuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string>("invoked", (id, time) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    Assert.Throws<AggregateException>(() => connection.Start(host).Wait());
                }
            }
        }

        [Fact]
        public async Task AuthenticatedUserCanInvokeMethodsInAuthorizedHubs()
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("AuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string>("invoked", (id, time) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    hub.InvokeWithTimeout("InvokedFromClient");

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
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
                    app.MapSignalR("/signalr", configuration);
                });
                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("InheritAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string, object>("joined", (id, time, authInfo) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    Assert.Throws<AggregateException>(() => connection.Start(host).Wait());
                }
            }
        }

        [Fact]
        public async Task AuthenticatedUserCanReceiveHubMessagesFromHubsInheritingFromAuthorizedHubs()
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("InheritAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string, object>("joined", (id, time, authInfo) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("InheritAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string>("invoked", (id, time) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    Assert.Throws<AggregateException>(() => connection.Start(host).Wait());
                }
            }
        }

        [Fact]
        public async Task AuthenticatedUserCanInvokeMethodsInHubsInheritingFromAuthorizedHubs()
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("InheritAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string>("invoked", (id, time) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    hub.InvokeWithTimeout("InvokedFromClient");

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("AdminAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string, object>("joined", (id, time, authInfo) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    Assert.Throws<AggregateException>(() => connection.Start(host).Wait());
                }
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("AdminAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string>("invoked", (id, time) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    Assert.Throws<AggregateException>(() => connection.Start(host).Wait());
                }
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("AdminAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string, object>("joined", (id, time, authInfo) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    Assert.Throws<AggregateException>(() => connection.Start(host).Wait());
                }
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("AdminAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string>("invoked", (id, time) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    Assert.Throws<AggregateException>(() => connection.Start(host).Wait());
                }
            }
        }

        [Fact]
        public async Task AuthenticatedAndAuthorizedUserCanReceiveHubMessagesFromHubsAuthorizedWithRoles()
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("AdminAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string, object>("joined", (id, time, authInfo) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
            }
        }

        [Fact]
        public async Task AuthenticatedAndAuthorizedUserCanInvokeMethodsInHubsAuthorizedWithRoles()
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("AdminAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string>("invoked", (id, time) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    hub.InvokeWithTimeout("InvokedFromClient");

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("UserAndRoleAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string, object>("joined", (id, time, authInfo) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    Assert.Throws<AggregateException>(() => connection.Start(host).Wait());
                }
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("UserAndRoleAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string>("invoked", (id, time) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    Assert.Throws<AggregateException>(() => connection.Start(host).Wait());
                }
            }
        }

        [Fact]
        public async Task AuthenticatedAndAuthorizedUserCanReceiveHubMessagesFromHubsAuthorizedSpecifyingUserAndRole()
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("UserAndRoleAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string, object>("joined", (id, time, authInfo) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
            }
        }

        [Fact]
        public async Task AuthenticatedAndAuthorizedUserCanInvokeMethodsInHubsAuthorizedSpecifyingUserAndRole()
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("UserAndRoleAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string>("invoked", (id, time) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    hub.InvokeWithTimeout("InvokedFromClient");

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
            }
        }

        [Fact]
        public async Task UnauthenticatedUserCanReceiveHubMessagesFromIncomingAuthorizedHubs()
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("IncomingAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string, object>("joined", (id, time, authInfo) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
            }
        }

        [Fact]
        public async Task UnauthenticatedUserCannotInvokeMethodsInIncomingAuthorizedHubs()
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("IncomingAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string>("invoked", (id, time) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    Assert.Throws<AggregateException>(() => hub.InvokeWithTimeout("InvokedFromClient"));

                    Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
            }
        }

        [Fact]
        public async Task AuthenticatedUserCanReceiveHubMessagesFromIncomingAuthorizedHubs()
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("IncomingAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string, object>("joined", (id, time, authInfo) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
            }
        }

        [Fact]
        public async Task AuthenticatedUserCanInvokeMethodsInIncomingAuthorizedHubs()
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("IncomingAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string>("invoked", (id, time) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    hub.InvokeWithTimeout("InvokedFromClient");

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
            }
        }

        [Fact]
        public async Task UnauthenticatedUserCannotInvokeAuthorizedHubMethods()
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("InvokeAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string>("invoked", (id, time) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    Assert.Throws<AggregateException>(() => hub.InvokeWithTimeout("InvokedFromClient"));

                    Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
            }
        }

        [Fact]
        public async Task UnauthorizedUserCannotInvokeAuthorizedHubMethods()
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("InvokeAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string>("invoked", (id, time) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    Assert.Throws<AggregateException>(() => hub.InvokeWithTimeout("InvokedFromClient"));

                    Assert.False(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
            }
        }

        [Fact]
        public async Task AuthenticatedAndAuthorizedUserCanInvokeAuthorizedHubMethods()
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
                    app.MapSignalR("/signalr", configuration);
                });

                var connection = CreateHubConnection("http://foo/");

                using (connection)
                {
                    var hub = connection.CreateHubProxy("InvokeAuthHub");
                    var wh = new ManualResetEvent(false);
                    hub.On<string, string>("invoked", (id, time) =>
                    {
                        Assert.NotNull(id);
                        wh.Set();
                    });

                    await connection.Start(host);

                    hub.InvokeWithTimeout("InvokedFromClient");

                    Assert.True(wh.WaitOne(TimeSpan.FromSeconds(3)));
                }
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
