using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.EventHub
{
    internal class ReceiverContext
    {
        public readonly EventHubReceiver Receiver;
        public readonly EventHubConnectionContext ConnectionContext;

        public int PartitionIndex { get; private set; }

        public ReceiverContext(int partitionIndex,
                               EventHubReceiver receiver,
                               EventHubConnectionContext connectionContext)
        {
            PartitionIndex = partitionIndex;
            Receiver = receiver;
            ConnectionContext = connectionContext;
        }

        public void OnError(Exception ex)
        {
            ConnectionContext.ErrorHandler(PartitionIndex, ex);
        }

        public void OnMessage(IEnumerable<EventData> messages)
        {
            ConnectionContext.EventDataHandler(PartitionIndex, messages);
        }
    }
}
