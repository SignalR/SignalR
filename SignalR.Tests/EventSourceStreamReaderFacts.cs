using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Client.Transports.ServerSentEvents;
using Xunit;

namespace SignalR.Tests
{
    public class EventSourceStreamReaderFacts
    {
        [Fact]
        public void ReadTriggersOpenedOnOpen()
        {
            var memoryStream = MemoryStream("data:somedata\n\n");
            var wh = new ManualResetEvent(false);
            var tcs = new TaskCompletionSource<string>();
            var eventSource = new EventSourceStreamReader(memoryStream);

            eventSource.Opened = () => wh.Set();
            eventSource.Message = sseEvent => tcs.TrySetResult(sseEvent.Data);

            eventSource.Start();
            Assert.True(wh.WaitOne(TimeSpan.FromSeconds(5)));
            Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(5)));
            Assert.Equal("somedata", tcs.Task.Result);
        }

        private MemoryStream MemoryStream(string data)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(data));
        }
    }
}
