using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR
{
    internal sealed class SendInfo
    {
        public int StreamIndex { get; set; }

        public IList<Message> Messages { get; set; }
    }
}