using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class ConnectionFacts
    {
        [Fact]
        public void SendingCommandObjectSetsCommandOnBus()
        {
            var messageBus = new Mock<IMessageBus>();
            var counters = new PerformanceCounterManager();
            Message message = null;
            messageBus.Setup(m => m.Publish(It.IsAny<Message>())).Returns<Message>(m =>
            {
                message = m;
                return TaskAsyncHelper.Empty;
            });

            var serializer = JsonUtility.CreateDefaultSerializer();
            var traceManager = new Mock<ITraceManager>();
            var connection = new Connection(messageBus.Object,
                                            serializer,
                                            "signal",
                                            "connectonid",
                                            new[] { "a", "signal", "connectionid" },
                                            new string[] { },
                                            traceManager.Object,
                                            new AckHandler(completeAcksOnTimeout: false,
                                                           ackThreshold: TimeSpan.Zero,
                                                           ackInterval: TimeSpan.Zero),
                                            counters,
                                            new Mock<IProtectedData>().Object);

            connection.Send("a", new Command
            {
                CommandType = CommandType.AddToGroup,
                Value = "foo"
            });

            Assert.NotNull(message);
            Assert.True(message.IsCommand);
            var command = serializer.Parse<Command>(message.Value, message.Encoding);
            Assert.Equal(CommandType.AddToGroup, command.CommandType);
            Assert.Equal("foo", command.Value);
        }

        [Fact]
        public void AcksAreSentToAckSubscriber()
        {
            // Arrange
            var waitCommand = new Command { WaitForAck = true };
            var ackerId = "acker";
            var waiterId = "waiter";
            var messageId = "messageId";
            var maxMessages = 1;

            var ackHandler = new Mock<IAckHandler>();
            ackHandler.Setup(m => m.TriggerAck(waitCommand.Id)).Returns(false);

            var messageBus = new Mock<IMessageBus>();
            Message waitMessage = null;
            Message ackMessage = null;
            messageBus.Setup(m => m.Publish(It.IsAny<Message>())).Returns<Message>(m =>
            {
                if (m.WaitForAck)
                {
                    waitMessage = m;
                }
                else if (m.IsAck)
                {
                    ackMessage = m;
                }

                return TaskAsyncHelper.Empty;
            });

            var counters = new PerformanceCounterManager();

            var serializer = JsonUtility.CreateDefaultSerializer();
            var traceManager = new Mock<ITraceManager>();
            var waiterConnection = new Connection(messageBus.Object,
                                                  serializer,
                                                  "signal",
                                                  waiterId,
                                                  new string[] { },
                                                  new string[] { },
                                                  traceManager.Object,
                                                  ackHandler.Object,
                                                  counters,
                                                  new Mock<IProtectedData>().Object);

            // Act
            waiterConnection.Send(ackerId, waitCommand);

            // Assert
            Assert.NotNull(waitMessage);
            Assert.Equal(waiterId, waitMessage.Source);
            Assert.Equal(PrefixHelper.GetConnectionId(ackerId), waitMessage.Key);

            // Arrange some more now that we have a waitMessage
            var messages = new List<ArraySegment<Message>>()
            {
                new ArraySegment<Message>(new[] { waitMessage })
            };
            var messageResult = new MessageResult(messages, 1);

            var ackerConnection = new Connection(messageBus.Object,
                                                 serializer,
                                                 "signal",
                                                 ackerId,
                                                 new string[] { },
                                                 new string[] { },
                                                 traceManager.Object,
                                                 ackHandler.Object,
                                                 counters,
                                                 new Mock<IProtectedData>().Object);
            ackerConnection.WriteCursor = _ => { };

            messageBus.Setup(m => m.Subscribe(ackerConnection,
                                              messageId,
                                              It.IsAny<Func<MessageResult, object, Task<bool>>>(),
                                              maxMessages,
                                              It.IsAny<object>()))
                .Callback<ISubscriber,
                          string,
                          Func<MessageResult, object, Task<bool>>,
                          int,
                          object>((subsciber, cursor, callback, max, state) =>
                {
                    callback(messageResult, state);
                });

            // Act
            ackerConnection.Receive(messageId, (_, __) => TaskAsyncHelper.False, maxMessages, null);

            // Assert
            Assert.NotNull(ackMessage);
            Assert.Equal(ackerId, ackMessage.Source);
            Assert.Equal(AckSubscriber.Signal, ackMessage.Key);
        }
    }
}
