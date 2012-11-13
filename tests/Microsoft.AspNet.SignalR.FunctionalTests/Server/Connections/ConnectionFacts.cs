using Moq;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Hosting.Memory;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Tests
{
    public class ConnectionFacts
    {
        public class Start : IDisposable
        {           
            [Fact]
            public void ThrownWebExceptionShouldBeUnwrapped()
            {
                using (var host = new MemoryHost())
                {
                    host.MapConnection<MyBadConnection>("/ErrorsAreFun");

                    var connection = new Client.Connection("http://test/ErrorsAreFun");

                    // Expecting 404
                    var aggEx = Assert.Throws<AggregateException>(() => connection.Start(new ServerSentEventsTransport(host)).Wait());

                    connection.Stop();

                    using (var ser = aggEx.GetError())
                    {
                        Assert.Equal(ser.StatusCode, HttpStatusCode.NotFound);
                        Assert.NotNull(ser.ResponseBody);
                        Assert.NotNull(ser.Exception);
                    }
                }
            }

            public class MyBadConnection : PersistentConnection
            {
                protected override Task OnConnectedAsync(IRequest request, string connectionId)
                {
                    // Should throw 404
                    using (HttpWebRequest.Create("http://www.microsoft.com/mairyhadalittlelambbut_shelikedhertwinkling_littlestar_better").GetResponse()) { }

                    return base.OnConnectedAsync(request, connectionId);
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
        }
    }
}
