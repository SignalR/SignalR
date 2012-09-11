using Moq;
using SignalR.Infrastructure;
using Xunit;

namespace SignalR.Tests.Server
{
    public class ConnectionFacts
    {
        [Fact]
        public void SendingCommandObjectSetsCommandOnBus()
        {
            var messageBus = new Mock<IMessageBus>();
            var counters = new Mock<IPerformanceCounterWriter>();
            Message message = null;
            messageBus.Setup(m => m.Publish(It.IsAny<Message>())).Returns<Message>(m =>
            {
                message = m;
                return TaskAsyncHelper.Empty;
            });

            var serializer = new JsonNetSerializer();
            var traceManager = new Mock<ITraceManager>();
            var connection = new Connection(messageBus.Object,
                                            serializer,
                                            "signal",
                                            "connectonid",
                                            new[] { "a", "signal", "connectionid" },
                                            new string[] { },
                                            traceManager.Object,
                                            counters.Object);

            connection.Send("a", new Command
            {
                Type = CommandType.AddToGroup,
                Value = "foo"
            });

            Assert.NotNull(message);
            Assert.True(message.IsCommand);
            var command = serializer.Parse<Command>(message.Value);
            Assert.Equal(CommandType.AddToGroup, command.Type);
            Assert.Equal("foo", command.Value);
        }
    }
}
