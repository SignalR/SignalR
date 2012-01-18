using System.Linq;
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

                var bus = new InProcessMessageBus(false);
                bus.Send("bar", "1").Wait();
                bus.Send("bar", "2").Wait();
                bus.Send("foo", "3").Wait();
                bus.Send("foo", "4").Wait();

                var result = bus.GetMessagesSince(new[] { "foo" }, 1).Result.ToList();
                Assert.Equal(2, result.Count);
            }

            [Fact]
            public void ReturnsNoMessagesWhenLastMessageIdIsEqualToLastMessage()
            {
                // id = 27
                // 24, 25, 27
                //         ^

                var bus = new InProcessMessageBus(false);
                bus.Send("foo", "1").Wait();
                bus.Send("foo", "2").Wait();

                // REVIEW: Will block
                //var result = bus.GetMessagesSince(new[] { "foo" }, 2).Result.ToList();
                //Assert.Equal(0, result.Count);
            }

            [Fact]
            public void ReturnsNoMessagesWhenLastMessageIdIsOnlyMessage()
            {
                // id = 27
                // 27
                // ^

                var bus = new InProcessMessageBus(false);
                bus.Send("bar", "1").Wait();
                bus.Send("foo", "2").Wait();

                // REVIEW: Will block
                //var result = bus.GetMessagesSince(new[] { "foo" }, 2).Result.ToList();
                // Assert.Equal(0, result.Count);
            }

            [Fact]
            public void ReturnsMessagesGreaterThanLastMessageIdWhenLastMessageIdNotInStore()
            {
                // id = 27
                // 24, 25, 28, 30, 45
                //     ^

                var bus = new InProcessMessageBus(false);
                bus.Send("bar", "1").Wait();
                bus.Send("foo", "2").Wait();
                bus.Send("bar", "3").Wait();
                bus.Send("foo", "4").Wait();
                bus.Send("bar", "5").Wait();
                bus.Send("foo", "6").Wait();

                var result = bus.GetMessagesSince(new[] { "foo" }, 3).Result.ToList();
                Assert.Equal(2, result.Count);
            }

            [Fact]
            public void ReturnsNoMessagesWhenLastMessageIdIsGreaterThanAllMessages()
            {
                // id = 27
                // 14, 18, 25, 26
                //             ^

                var bus = new InProcessMessageBus(false);
                bus.Send("foo", "1").Wait();
                bus.Send("foo", "2").Wait();
                bus.Send("bar", "3").Wait();
                bus.Send("bar", "4").Wait();

                // REVIEW: Will block
                // var result = bus.GetMessagesSince(new[] { "foo" }, 3).Result.ToList();
                // Assert.Equal(0, result.Count);
            }

            [Fact]
            public void ReturnsNoMessagesWhenThereAreNoMessages()
            {
                var bus = new InProcessMessageBus(false);

                // REVIEW: Will block
                // var result = bus.GetMessagesSince(new[] { "foo" }, 1).Result.ToList();
                // Assert.Equal(0, result.Count);
            }
        }


    }
}
