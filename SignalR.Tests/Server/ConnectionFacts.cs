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
            var messageBus = new Mock<INewMessageBus>();
            Message message = null;
            messageBus.Setup(m => m.Publish(It.IsAny<Message>())).Callback<Message>(m => message = m);
            var serializer = new JsonNetSerializer();
            var traceManager = new Mock<ITraceManager>();
            var connection = new Connection(messageBus.Object,
                                            serializer,
                                            "signal",
                                            "connectonid",
                                            new[] { "a", "signal", "connectionid" },
                                            new string[] { },
                                            traceManager.Object);

            connection.Send("a", new Command
            {
                Type = CommandType.AddToGroup,
                Value = "foo"
            });

            Assert.NotNull(message);
            Assert.True(message.IsCommand);
            var command = serializer.Parse<Command>(message.Value);
            Assert.Equal(CommandType.AddToGroup, command.Type);
            Assert.Equal(@"{""Name"":""foo"",""Cursor"":null}", command.Value);
        }
    }
}
