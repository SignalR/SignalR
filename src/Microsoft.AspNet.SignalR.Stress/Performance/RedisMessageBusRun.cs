// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

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
            var configuration = new RedisScaleoutConfiguration2("127.0.0.1", 6379, "", "Stress");
            // configuration.RetryOnError = true;

            return new RedisMessageBus(Resolver, configuration);
        }
    }
}
