using System;
using System.Threading;
using SignalR.Client.Hubs;
using SignalR.Hosting.Memory;
using Xunit;

namespace SignalR.Tests
{
    public class HubFacts
    {
        [Fact]
        public void SettingState()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            hub["name"] = "test";

            connection.Start(host).Wait();

            var result = hub.Invoke<string>("ReadStateValue").Result;

            Assert.Equal("test", result);
        }

        [Fact]
        public void GetValueFromServer()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            connection.Start(host).Wait();

            var result = hub.Invoke<int>("GetValue").Result;

            Assert.Equal(10, result);
        }

        [Fact]
        public void TaskWithException()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            connection.Start(host).Wait();

            var ex = Assert.Throws<AggregateException>(() => hub.Invoke("TaskWithException").Wait());

            Assert.Equal("Exception of type 'System.Exception' was thrown.", ex.GetBaseException().Message);
        }

        [Fact]
        public void GenericTaskWithException()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            connection.Start(host).Wait();

            var ex = Assert.Throws<AggregateException>(() => hub.Invoke("GenericTaskWithException").Wait());

            Assert.Equal("Exception of type 'System.Exception' was thrown.", ex.GetBaseException().Message);
        } 

        [Fact]
        public void Overloads()
        {
            var host = new MemoryHost();
            host.MapHubs();
            var connection = new Client.Hubs.HubConnection("http://foo/");

            var hub = connection.CreateProxy("demo");

            connection.Start(host).Wait();

            hub.Invoke("Overload").Wait();
            int n = hub.Invoke<int>("Overload", 1).Result;

            Assert.Equal(1, n);
        }

        [Fact]
        public void ChangeHubUrl()
        {
            var host = new MemoryHost();
            host.MapHubs("/foo");
            var connection = new Client.Hubs.HubConnection("http://site/foo", useDefaultUrl: false);

            var hub = connection.CreateProxy("demo");

            var wh = new ManualResetEvent(false);

            hub.On("signal", id =>
            {
                Assert.NotNull(id);
                wh.Set();
            });
            
            connection.Start(host).Wait();

            hub.Invoke("DynamicTask").Wait();

            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(5)));
        }
    }
}
