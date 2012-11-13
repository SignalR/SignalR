using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using Microsoft.AspNet.SignalR.Hubs;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class HubProxyFacts : IDisposable
    {
        [Fact]
        public void EndToEndTest()
        {
            using (var host = new MemoryHost())
            {
                host.MapHubs();

                var hubConnection = new HubConnection("http://fake");
                IHubProxy proxy = hubConnection.CreateHubProxy("ChatHub");
                var wh = new ManualResetEvent(false);

                proxy.On("addMessage", data =>
                {
                    Assert.Equal("hello", data);
                    wh.Set();
                });

                hubConnection.Start(host).Wait();

                proxy.Invoke("Send", "hello").Wait();

                Assert.True(wh.WaitOne(TimeSpan.FromSeconds(5)));
            }
        }

        [Fact]
        public void HubNamesAreNotCaseSensitive()
        {
            using (var host = new MemoryHost())
            {
                host.MapHubs();

                var hubConnection = new HubConnection("http://fake");
                IHubProxy proxy = hubConnection.CreateHubProxy("chatHub");
                var wh = new ManualResetEvent(false);

                proxy.On("addMessage", data =>
                {
                    Assert.Equal("hello", data);
                    wh.Set();
                });

                hubConnection.Start(host).Wait();

                proxy.Invoke("Send", "hello").Wait();

                Assert.True(wh.WaitOne(TimeSpan.FromSeconds(5)));
            }
        }

        [Fact]
        public void UnableToCreateHubThrowsError()
        {
            using (var host = new MemoryHost())
            {
                host.MapHubs();

                var hubConnection = new HubConnection("http://fake");
                IHubProxy proxy = hubConnection.CreateHubProxy("MyHub2");

                hubConnection.Start(host).Wait();
                Assert.Throws<MissingMethodException>(() => proxy.Invoke("Send", "hello").Wait());
            }
        }
        
        public class MyHub2 : Hub
        {
            public MyHub2(int n)
            {

            }

            public void Send(string value)
            {

            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public class ChatHub : Hub
        {
            public Task Send(string message)
            {
                return Clients.All.addMessage(message);
            }
        }
    }
}
