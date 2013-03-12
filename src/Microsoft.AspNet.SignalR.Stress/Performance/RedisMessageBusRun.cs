using System.ComponentModel.Composition;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Redis;

namespace Microsoft.AspNet.SignalR.Stress.Performance
{
    [Export("RedisMessageBus", typeof(IRun))]
    public class RedisMessageBusRun : MessageBusRun
    {
        [ImportingConstructor]
        public RedisMessageBusRun(RunData runData)
            : base(runData)
        {

        }

        protected override MessageBus CreateMessageBus()
        {
            return new RedisMessageBus("127.0.0.1", 6379, "", 0, "Stress", Resolver);
        }
    }
}
