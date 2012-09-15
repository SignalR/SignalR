using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SignalR.Tests.Server
{
    public class ScaleOutMessageBusFacts
    {
        [Fact]
        public void Foo()
        {
            var dr = new DefaultDependencyResolver();
            var bus = new MyBus(dr);

            bus.Send(new Message("test", "key", "1"));
            bus.Send(new Message("test", "key", "1"));
            bus.Send(new Message("test", "key", "1"));
        }

        private class MyBus : ScaleoutMessageBus
        {
            private int _id;
            public MyBus(IDependencyResolver resolver)
                : base(resolver)
            {
            }

            protected override void Initialize()
            {
                
            }

            public Task Send(Message messages)
            {
                return Send(new[] { messages });
            }

            protected override Task Send(Message[] messages)
            {
                return OnReceived("0", (ulong)Interlocked.Increment(ref _id), messages);
            }
        }
    }
}
