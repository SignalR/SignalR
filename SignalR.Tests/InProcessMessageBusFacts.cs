using System.Threading;
using SignalR.Infrastructure;
using Xunit;

namespace SignalR.Tests
{
    public class InProcessMessageBusFacts
    {
        public class GetMessagesSince
        {
            [Fact]
            public void ReturnsAllMessagesWhenLastMessageIdIsLessThanAllMessages()
            {
                //    id = 27
                // _, 28, 29, 32
                // ^

                var trace = new TraceManager();
                var bus = new InProcessMessageBus(trace, false);
                bus.Send("testclient", "bar", "1").Wait();
                bus.Send("testclient", "bar", "2").Wait();
                bus.Send("testclient", "foo", "3").Wait();
                bus.Send("testclient", "foo", "4").Wait();

                var result = bus.GetMessages(new[] { "foo" }, "1", CancellationToken.None).Result;
                Assert.Equal(2, result.Messages.Length);
            }

            [Fact]
            public void ReturnsMessagesGreaterThanLastMessageIdWhenLastMessageIdNotInStore()
            {
                // id = 27
                // 24, 25, 28, 30, 45
                //     ^

                var trace = new TraceManager();
                var bus = new InProcessMessageBus(trace, false);
                bus.Send("testclient", "bar", "1").Wait();
                bus.Send("testclient", "foo", "2").Wait();
                bus.Send("testclient", "bar", "3").Wait();
                bus.Send("testclient", "foo", "4").Wait();
                bus.Send("testclient", "bar", "5").Wait();
                bus.Send("testclient", "foo", "6").Wait();

                var result = bus.GetMessages(new[] { "foo" }, "3", CancellationToken.None).Result;
                Assert.Equal(2, result.Messages.Length);
            }

            [Fact]
            public void GetAllSinceReturnsAllMessagesIfIdGreaterThanMaxId()
            {
                var trace = new TraceManager();
                var bus = new InProcessMessageBus(trace, false);

                for (int i = 0; i < 10; i++)
                {
                    bus.Send("testclient", "a", i).Wait();
                }

                var result = bus.GetMessages(new[] { "a" }, "100", CancellationToken.None).Result;
                Assert.Equal(10, result.Messages.Length);
                for (int i = 0; i < 10; i++)
                {
                    Assert.Equal(i, result.Messages[i].Value);
                }
            }
        }
    }
}
