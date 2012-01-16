using System.Linq;
using Xunit;
using System.Globalization;

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
    }
}
