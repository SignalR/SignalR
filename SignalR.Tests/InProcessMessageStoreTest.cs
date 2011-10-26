using System.Linq;
using Xunit;

namespace SignalR.Tests
{
    public class InProcessMessageStoreTest
    {
        [Fact]
        public void GetAllSinceReturnsAllMessagesAfterIdOrderedById()
        {
            var store = new InProcessMessageStore(garbageCollectMessages: false);

            store.Save("a", "1").Wait();
            store.Save("a", "2").Wait();
            store.Save("a", "3").Wait();

            var messages = store.GetAllSince("a", 1).Result.ToList();
            Assert.Equal(2, messages.Count);
            Assert.Equal("2", messages[0].Value);
            Assert.Equal("3", messages[1].Value);
        }

        [Fact]
        public void GetAllSinceReturnsAllMessagesIfIdGreaterThanMaxId()
        {
            var store = new InProcessMessageStore(garbageCollectMessages: false);

            for (int i = 0; i < 10; i++)
            {
                store.Save("a", i).Wait();
            }

            var messages = store.GetAllSince("a", 100).Result.ToList();
            Assert.Equal(10, messages.Count);
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(i, messages[i].Value);
            }
        }

        [Fact]
        public void GetLastIdReturnsMaxMessageId()
        {
            var store = new InProcessMessageStore(garbageCollectMessages: false);

            store.Save("a", "1").Wait();
            store.Save("a", "2").Wait();
            store.Save("a", "3").Wait();

            Assert.Equal(3, store.GetLastId().Result);
        }
    }
}
