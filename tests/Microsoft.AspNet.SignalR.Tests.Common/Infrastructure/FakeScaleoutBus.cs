using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.Tests.Common.Infrastructure
{
    public class FakeScaleoutBus : ScaleoutMessageBus
    {
        private int _streams;
        private ulong _id;

        public FakeScaleoutBus(IDependencyResolver resolver)
            : this(resolver, streams: 1)
        {
        }

        public FakeScaleoutBus(IDependencyResolver dr, int streams)
            : base(dr, new ScaleoutConfiguration())
        {
            _streams = streams;

            for (int i = 0; i < _streams; i++)
            {
                Open(i);
            }
        }

        protected override int StreamCount
        {
            get
            {
                return _streams;
            }
        }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            var message = new ScaleoutMessage(messages);

            OnReceived(streamIndex, _id, message);

            _id++;

            return TaskAsyncHelper.Empty;
        }
    }
}
