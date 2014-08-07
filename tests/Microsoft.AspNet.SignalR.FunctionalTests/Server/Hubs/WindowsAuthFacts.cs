using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.Owin.Hosting;
using Owin;
using Xunit;
using Xunit.Extensions;
using TransportType = Microsoft.AspNet.SignalR.Tests.Common.Infrastructure.TransportType;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Server.Hubs
{
    public class WindowsAuthFacts
    {
        [Theory]
        [InlineData(TransportType.LongPolling)]
        [InlineData(TransportType.ServerSentEvents)]
        [InlineData(TransportType.Websockets)]
        public async Task WindowsIdentityIsUsableInOnDisconnected(TransportType transportType)
        {
            IClientTransport transport;
            switch (transportType)
            {
                case TransportType.LongPolling:
                    transport = new LongPollingTransport();
                    break;
                case TransportType.ServerSentEvents:
                    transport = new ServerSentEventsTransport();
                    break;
                case TransportType.Websockets:
                    transport = new WebSocketTransport();
                    break;
                default:
                    throw new ArgumentException();
            }

            var windowsAuthHub = new WindowsAuthHub();
            Action<IAppBuilder> configure = app =>
            {
                HttpListener listener = (HttpListener)app.Properties["System.Net.HttpListener"];
                listener.AuthenticationSchemes = AuthenticationSchemes.IntegratedWindowsAuthentication;

                var resolver = new DefaultDependencyResolver();
                resolver.Register(typeof(WindowsAuthHub), () => windowsAuthHub);

                app.MapSignalR(new HubConfiguration() { Resolver = resolver });
            };

            var url = "http://localhost:8000";
            using (WebApp.Start(url, configure))
            using (var connection = new HubConnection(url))
            {
                connection.Credentials = CredentialCache.DefaultCredentials;

                var proxy = connection.CreateHubProxy("WindowsAuthHub");
                await connection.Start(transport);
            }

            Assert.True(windowsAuthHub.UserName.Wait(TimeSpan.FromSeconds(10)));
            Assert.False(string.IsNullOrEmpty(windowsAuthHub.UserName.Result));
        }

        private class WindowsAuthHub : Hub
        {
            private TaskCompletionSource<string> _nameTcs = new TaskCompletionSource<string>();

            public Task<string> UserName
            {
                get { return _nameTcs.Task; }
            }

            public override Task OnDisconnected(bool stopCalled)
            {
                if (Context.User is WindowsPrincipal)
                {
                    _nameTcs.TrySetResult(Context.User.Identity.Name);
                }
                else
                {
                    _nameTcs.TrySetException(new Exception());
                }

                return base.OnDisconnected(stopCalled);
            }
        }
    }
}
