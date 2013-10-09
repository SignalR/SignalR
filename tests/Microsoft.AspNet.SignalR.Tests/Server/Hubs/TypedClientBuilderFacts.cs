using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;
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

            mockClientProxy.Verify();
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
            client.ping().Wait();

            mockClientProxy.Verify();
        }

        [Fact]
        public void InvalidReturnTypesAreRejected()
        {
            var mockClientProxy = new Mock<IClientProxy>(MockBehavior.Strict);

            Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IDontReturnVoidOrTask>.Build(mockClientProxy.Object));
            Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IHaveOutParameter>.Build(mockClientProxy.Object));
            Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IHaveRefParameter>.Build(mockClientProxy.Object));
            Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IHaveProperties>.Build(mockClientProxy.Object));
            Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IHaveIndexer>.Build(mockClientProxy.Object));
            Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<IHaveEvent>.Build(mockClientProxy.Object));
            Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<NotAnInterface>.Build(mockClientProxy.Object));
            Assert.Throws<InvalidOperationException>(() => TypedClientBuilder<AlsoNotAnInterface>.Build(mockClientProxy.Object));

            mockClientProxy.Verify();
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
            Task ping();
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
}
