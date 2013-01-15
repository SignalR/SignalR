using System;
using Microsoft.AspNet.SignalR.Hosting;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class PersistentConnectionFactoryFacts
    {
        public class CreateInstance
        {
            [Fact]
            public void ThrowsIfTypeIsNull()
            {
                // Arrange
                var resolver = new Mock<IDependencyResolver>();
                var factory = new PersistentConnectionFactory(resolver.Object);

                // Act & Assert
                Assert.Throws<ArgumentNullException>(() => factory.CreateInstance(null));
            }

            [Fact]
            public void ThrowsIfTypeIsNotPersistentConnection()
            {
                // Arrange
                var resolver = new Mock<IDependencyResolver>();
                var factory = new PersistentConnectionFactory(resolver.Object);

                // Act & Assert
                Assert.Throws<InvalidOperationException>(() => factory.CreateInstance(typeof(PersistentConnectionFactoryFacts)));
            }

            [Fact]
            public void CreatesInstanceIfTypeIsPersistentConnection()
            {
                // Arrange
                var resolver = new Mock<IDependencyResolver>();
                var factory = new PersistentConnectionFactory(resolver.Object);

                // Act
                PersistentConnection connection = factory.CreateInstance(typeof(MyConnection));

                // Assert
                Assert.NotNull(connection);
            }

            [Fact]
            public void UsesDependencyResolver()
            {
                // Arrange
                var resolver = new Mock<IDependencyResolver>();
                var factory = new PersistentConnectionFactory(resolver.Object);
                var otherConnection = new MyOtherConnection();
                resolver.Setup(m => m.GetService(typeof(MyConnection)))
                        .Returns(otherConnection);

                // Act
                PersistentConnection connection = factory.CreateInstance(typeof(MyConnection));

                // Assert
                Assert.NotNull(connection);
                Assert.Same(otherConnection, connection);
            }

            public class MyOtherConnection : MyConnection
            {
    
            }

            public class MyConnection : PersistentConnection
            {
                
            }
        }
    }
}
