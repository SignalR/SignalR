// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Redis;
using Microsoft.AspNet.SignalR.ServiceBus;

namespace Microsoft.AspNet.SignalR.Stress.Performance
{
    [Export("ServiceBusMessageBus", typeof(IRun))]
    public class ServiceBusMessageBusRun : MessageBusRun
    {
        [ImportingConstructor]
        public ServiceBusMessageBusRun(RunData runData)
            : base(runData)
        {

        }

        protected override MessageBus CreateMessageBus()
        {
            var configuration = new ServiceBusScaleoutConfiguration(RunData.ServiceBusConnectionString, "Stress");
            // configuration.RetryOnError = true;

            return new ServiceBusMessageBus(Resolver, configuration);
        }
    }
}
