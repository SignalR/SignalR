﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            var configuration = new RedisScaleoutConfiguration(RunData.RedisServer, RunData.RedisPort, RunData.RedisPassword, "Stress");
            // configuration.MaxQueueLength = 1000;

            return new RedisMessageBus(Resolver, configuration, new RedisConnection());
        }
    }
}
