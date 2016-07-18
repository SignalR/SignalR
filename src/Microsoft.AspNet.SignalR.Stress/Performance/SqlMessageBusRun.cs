// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.SqlServer;

namespace Microsoft.AspNet.SignalR.Stress.Performance
{
    [Export("SqlMessageBus", typeof(IRun))]
    public class SqlMessageBusRun : MessageBusRun
    {
        [ImportingConstructor]
        public SqlMessageBusRun(RunData runData)
            : base(runData)
        {

        }

        protected override MessageBus CreateMessageBus()
        {
            var config = new SqlScaleoutConfiguration(RunData.SqlConnectionString) { TableCount = RunData.SqlTableCount };

            return new SqlMessageBus(Resolver, config);
        }
    }
}
