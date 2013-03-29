using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNet.SignalR.Messaging;

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    public class ServiceBusScaleoutConfiguration : ScaleoutConfiguration
    {
        public string ConnectionString { get; set; }

        public string TopicPrefix { get; set; }
        
        public int TopicCount { get; set; }
    }
}
