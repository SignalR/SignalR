using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Messaging;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class GroupManagerFacts
    {
        public class Add
        {
            [Fact]
            public void ThrowsIfConnectionIsNull()
            {
                Assert.Throws<ArgumentNullException>(() => new GroupManager(null, "Prefix"));
            }

            [Fact]
            public void ThrowsIfConnectionIdIsNull()
            {
                // Arrange
                var connection = new Mock<IConnection>();
                connection.Setup(m => m.Send(It.IsAny<ConnectionMessage>()));
                var groupManager = new GroupManager(connection.Object, "Prefix");

                // Assert
                Assert.Throws<ArgumentNullException>(() => groupManager.Add(null, "SomeGroup"));
            }

            [Fact]
            public void ThrowsIfGroupIsNull()
            {
                // Arrange
                var connection = new Mock<IConnection>();
                connection.Setup(m => m.Send(It.IsAny<ConnectionMessage>()));
                var groupManager = new GroupManager(connection.Object, "Prefix");

                // Assert
                Assert.Throws<ArgumentNullException>(() => groupManager.Add("1", null));
            }

            [Fact]
            public void CreatesAddToGroupCommandOnAdd()
            {
                // Arrange
                var connection = new Mock<IConnection>();
                connection.Setup(m => m.Send(It.IsAny<ConnectionMessage>()))
                          .Callback<ConnectionMessage>(m =>
                          {
                              Assert.Equal("c-1", m.Signal);
                              Assert.NotNull(m.Value);
                              var command = m.Value as Command;
                              Assert.NotNull(command);
                              Assert.NotNull(command.Id);
                              Assert.Equal(CommandType.AddToGroup, command.CommandType);
                              Assert.Equal("Prefix.MyGroup", command.Value);
                              Assert.True(command.WaitForAck);
                          });

                var groupManager = new GroupManager(connection.Object, "Prefix");

                // Act
                groupManager.Add("1", "MyGroup");
            }
        }

        public class Remove
        {
            [Fact]
            public void ThrowsIfConnectionIdIsNull()
            {
                // Arrange
                var connection = new Mock<IConnection>();
                connection.Setup(m => m.Send(It.IsAny<ConnectionMessage>()));
                var groupManager = new GroupManager(connection.Object, "Prefix");

                // Assert
                Assert.Throws<ArgumentNullException>(() => groupManager.Remove(null, "SomeGroup"));
            }

            [Fact]
            public void ThrowsIfGroupIsNull()
            {
                // Arrange
                var connection = new Mock<IConnection>();
                connection.Setup(m => m.Send(It.IsAny<ConnectionMessage>()));
                var groupManager = new GroupManager(connection.Object, "Prefix");

                // Assert
                Assert.Throws<ArgumentNullException>(() => groupManager.Remove("1", null));
            }

            [Fact]
            public void CreatesRemoveFromGroupCommandOnAdd()
            {
                // Arrange
                var connection = new Mock<IConnection>();
                connection.Setup(m => m.Send(It.IsAny<ConnectionMessage>()))
                          .Callback<ConnectionMessage>(m =>
                          {
                              Assert.Equal("c-1", m.Signal);
                              Assert.NotNull(m.Value);
                              var command = m.Value as Command;
                              Assert.NotNull(command);
                              Assert.NotNull(command.Id);
                              Assert.Equal(CommandType.RemoveFromGroup, command.CommandType);
                              Assert.Equal("Prefix.MyGroup", command.Value);
                              Assert.True(command.WaitForAck);
                          });

                var groupManager = new GroupManager(connection.Object, "Prefix");

                // Act
                groupManager.Remove("1", "MyGroup");
            }
        }

        public class Send
        {
            [Fact]
            public void ThrowsIfGroupIsNull()
            {
                // Arrange
                var connection = new Mock<IConnection>();
                connection.Setup(m => m.Send(It.IsAny<ConnectionMessage>()));
                var groupManager = new GroupManager(connection.Object, "Prefix");

                // Assert
                Assert.Throws<ArgumentException>(() => groupManager.Send((string)null, "Way"));
            }

            [Fact]
            public void ThrowsIfGroupsIsNull()
            {
                // Arrange
                var connection = new Mock<IConnection>();
                connection.Setup(m => m.Send(It.IsAny<ConnectionMessage>()));
                var groupManager = new GroupManager(connection.Object, "Prefix");

                // Assert
                Assert.Throws<ArgumentNullException>(() => groupManager.Send((IList<string>)null, "Way"));
            }

            [Fact]
            public void SendsMessageToGroup()
            {
                // Arrange
                var connection = new Mock<IConnection>();
                connection.Setup(m => m.Send(It.IsAny<ConnectionMessage>()))
                          .Callback<ConnectionMessage>(m =>
                          {
                              Assert.Equal("Prefix.MyGroup", m.Signal);
                              Assert.Equal("some value", m.Value);
                              Assert.True(m.ExcludedSignals.Contains("c-x"));
                              Assert.True(m.ExcludedSignals.Contains("c-y"));
                          });

                var groupManager = new GroupManager(connection.Object, "Prefix");

                // Act
                groupManager.Send("MyGroup", "some value", "x", "y");
            }
        }
    }
}
