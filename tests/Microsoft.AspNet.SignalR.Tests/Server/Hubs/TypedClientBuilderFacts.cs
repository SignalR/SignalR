using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Samples.Hubs.DemoHub;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server.Hubs
{
    public class TypedClientBuilderFacts
    {
        [Fact]
        public void MethodsAreInvokedThroughIClientProxy()
        {
            var mockClientProxy = new Mock<IClientProxy>(MockBehavior.Strict);
            mockClientProxy.Setup(c => c.Invoke("send", "fun!"))
                .Returns(Task.FromResult<object>(null));
            mockClientProxy.Setup(c => c.Invoke("sendMore", true, new[] { "more", "messages" }))
                .Returns(Task.FromResult<object>(null));
            mockClientProxy.Setup(c => c.Invoke("sendDefault", "default"))
                .Returns(Task.FromResult<object>(null));
            mockClientProxy.Setup(c => c.Invoke("ToString"))
                .Returns(Task.FromResult<object>(null));
            mockClientProxy.Setup(c => c.Invoke("ping"))
                .Returns(Task.FromResult<object>(null));

            var client = TypedClientBuilder<IClientContract>.Build(mockClientProxy.Object);

            client.send("fun!");
            client.sendMore(true, "more", "messages");
            client.sendDefault();
            client.ToString();
            client.ping().Wait();

            mockClientProxy.VerifyAll();
        }

        [Fact]
        public void MethodsAreInvokedThroughGenericInterface()
        {
            var mockClientProxy = new Mock<IClientProxy>(MockBehavior.Strict);
            mockClientProxy.Setup(c => c.Invoke("send", 42))
                .Returns(Task.FromResult<object>(null));
            mockClientProxy.Setup(c => c.Invoke("ping"))
                .Returns(Task.FromResult<object>(null));

            var client = TypedClientBuilder<IAmGeneric<int>>.Build(mockClientProxy.Object);

            client.send(42);
            client.ping();

            mockClientProxy.VerifyAll();
        }

        [Fact]
        public void MethodsAreInvokedThroughDerivedInterface()
        {
            var mockClientProxy = new Mock<IClientProxy>();

            var client = TypedClientBuilder<IAmDerived<int>>.Build(mockClientProxy.Object);

            // Invoke additional method defined in IAmDerived
            client.addedMethod();

            // Invoke "new" ping method defined in IAmDerived
            client.ping();
            ((IClientContract)client).ping();
            ((IAmGeneric<int>)client).ping();

            // Invoke send overload defined in multiple parent interfaces
            client.send("fun!");
            client.send(42);

            mockClientProxy.Verify(c => c.Invoke("addedMethod"), Times.Once());
            mockClientProxy.Verify(c => c.Invoke("ping"), Times.Exactly(3));
            mockClientProxy.Verify(c => c.Invoke("send", "fun!"), Times.Once());
            mockClientProxy.Verify(c => c.Invoke("send", 42), Times.Once());
        }

        [Fact]
        public void MethodsAreInvokedOnTwoInterfacesWithTheSameName()
        {
            var mockClientProxy1 = new Mock<IClientProxy>(MockBehavior.Strict);
            mockClientProxy1.Setup(c => c.Invoke("ping"))
                .Returns(Task.FromResult<object>(null));

            var client1 = TypedClientBuilder<IClientContract>.Build(mockClientProxy1.Object);
            client1.ping().Wait();
            mockClientProxy1.VerifyAll();


            var mockClientProxy2 = new Mock<IClientProxy>(MockBehavior.Strict);
            mockClientProxy2.Setup(c => c.Invoke("test"))
                .Returns(Task.FromResult<object>(null));

            var client2 = TypedClientBuilder<Test.IClientContract>.Build(mockClientProxy2.Object);
            client2.test();
            mockClientProxy2.VerifyAll();
        }

        [Fact]
        public void InvalidTypesAreRejected()
        {
            var mockClientProxy = Mock.Of<IClientProxy>();

            Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IDontReturnVoidOrTask>.Build(mockClientProxy));
            Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IHaveOutParameter>.Build(mockClientProxy));
            Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IHaveRefParameter>.Build(mockClientProxy));
            Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IHaveProperties>.Build(mockClientProxy));
            Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IHaveIndexer>.Build(mockClientProxy));
            Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IHaveEvent>.Build(mockClientProxy));
            Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IAmDerivedFromInvalidInterface>.Build(mockClientProxy));
            Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<NotAnInterface>.Build(mockClientProxy));
            Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<AlsoNotAnInterface>.Build(mockClientProxy));
        }

        [Fact]
        public void GetHubContextRejectsInvalidTypes()
        {
            var resolver = new DefaultDependencyResolver();
            var manager = resolver.Resolve<IConnectionManager>();

            Assert.Throws<InvalidOperationException>(() => manager.GetHubContext<DemoHub, IDontReturnVoidOrTask>());
            Assert.Throws<InvalidOperationException>(() => manager.GetHubContext<DemoHub, IHaveOutParameter>());
            Assert.Throws<InvalidOperationException>(() => manager.GetHubContext<DemoHub, IHaveRefParameter>());
            Assert.Throws<InvalidOperationException>(() => manager.GetHubContext<DemoHub, IHaveProperties>());
            Assert.Throws<InvalidOperationException>(() => manager.GetHubContext<DemoHub, IHaveIndexer>());
            Assert.Throws<InvalidOperationException>(() => manager.GetHubContext<DemoHub, IHaveEvent>());
            Assert.Throws<InvalidOperationException>(() => manager.GetHubContext<DemoHub, IAmDerivedFromInvalidInterface>());
            Assert.Throws<InvalidOperationException>(() => manager.GetHubContext<DemoHub, NotAnInterface>());
        }

        // Valid type parameters
        public interface IClientContract
        {
            void send(string messages);
            void sendMore(bool test, params string[] messages);
            void sendDefault(string message="default");
            void ToString();
            Task ping();
        }

        public interface IAmGeneric<T>
        {
            void send(T genericArgument);
            void ping();
        }

        public interface IAmDerived<T> : IClientContract, IAmGeneric<T>
        {
            void addedMethod();
            new void ping();
        }

        public interface IAmDerivedFromInvalidInterface : IDontReturnVoidOrTask
        {
        }

        // Invalid type parameters
        public interface IDontReturnVoidOrTask
        {
            void send(string messages);
            int add(int a, int b);
        }

        public interface IHaveOutParameter
        {
            void send(string messages);
            void status(string message, out bool isAlive);
        }

        public interface IHaveRefParameter
        {
            void send(string messages);
            void status(string message, ref bool isAlive);
        }

        public interface IHaveProperties
        {
            void send(string messages);
            int State { get; set; }
            bool AnotherProperty { get; set; }
        }

        public interface IHaveIndexer
        {
            void send(string messages);
            string this[int index] { get; set; }
        }

        public interface IHaveEvent
        {
            void send(string messages);
            event Action<string> callback;
        }

        public class NotAnInterface
        {
        }

        public struct AlsoNotAnInterface
        {
        }
    }

    namespace Test
    {
        public interface IClientContract
        {
            void test();
        }
    }
}
