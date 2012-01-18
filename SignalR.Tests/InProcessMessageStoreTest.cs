using System;
using System.Linq;
using Xunit;

namespace SignalR.Tests
{
    public class InProcessMessageStoreTest
    {
        [Fact]
        public void GetLastIdReturnsMaxMessageId()
        {
            var store = new InProcessMessageStore(garbageCollectMessages: false);

            store.Save("a", "1").Wait();
            store.Save("a", "2").Wait();
            store.Save("a", "3").Wait();

            Assert.Equal("3", store.GetLastId().Result);
        }

        public class GetAllSince
        {
            [Fact]
            public void ReturnsAllMessagesWhenLastMessageIdIsLessThanAllMessages()
            {
                //    id = 27
                // _, 28, 29, 32
                // ^

                var store = new InProcessMessageStore(false);
                store.Save("bar", "1").Wait();
                store.Save("bar", "2").Wait();
                store.Save("foo", "3").Wait();
                store.Save("foo", "4").Wait();

                var result = store.GetAllSince(new[] { "foo" }, "1").Result.ToList();
                Assert.Equal(2, result.Count);
            }

            [Fact]
            public void ReturnsNoMessagesWhenLastMessageIdIsEqualToLastMessage()
            {
                // id = 27
                // 24, 25, 27
                //         ^

                var store = new InProcessMessageStore(false);
                store.Save("foo", "1").Wait();
                store.Save("foo", "2").Wait();

                var result = store.GetAllSince(new[] { "foo" }, "2").Result.ToList();
                Assert.Equal(0, result.Count);
            }

            [Fact]
            public void ReturnsNoMessagesWhenLastMessageIdIsOnlyMessage()
            {
                // id = 27
                // 27
                // ^

                var store = new InProcessMessageStore(false);
                store.Save("bar", "1").Wait();
                store.Save("foo", "2").Wait();

                var result = store.GetAllSince(new[] { "foo" }, "2").Result.ToList();
                Assert.Equal(0, result.Count);
            }

            [Fact]
            public void ReturnsMessagesGreaterThanLastMessageIdWhenLastMessageIdNotInStore()
            {
                // id = 27
                // 24, 25, 28, 30, 45
                //     ^

                var store = new InProcessMessageStore(false);
                store.Save("bar", "1").Wait();
                store.Save("foo", "2").Wait();
                store.Save("bar", "3").Wait();
                store.Save("foo", "4").Wait();
                store.Save("bar", "5").Wait();
                store.Save("foo", "6").Wait();

                var result = store.GetAllSince(new[] { "foo" }, "3").Result.ToList();
                Assert.Equal(2, result.Count);
            }

            [Fact]
            public void ReturnsNoMessagesWhenLastMessageIdIsGreaterThanAllMessages()
            {
                // id = 27
                // 14, 18, 25, 26
                //             ^

                var store = new InProcessMessageStore(false);
                store.Save("foo", "1").Wait();
                store.Save("foo", "2").Wait();
                store.Save("bar", "3").Wait();
                store.Save("bar", "4").Wait();

                var result = store.GetAllSince(new[] { "foo" }, "3").Result.ToList();
                Assert.Equal(0, result.Count);
            }

            [Fact]
            public void ReturnsNoMessagesWhenThereAreNoMessages()
            {
                var store = new InProcessMessageStore(false);

                var result = store.GetAllSince(new[] { "foo" }, "1").Result.ToList();
                Assert.Equal(0, result.Count);
            }
        }


    }
}
