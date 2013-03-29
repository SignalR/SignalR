// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

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
            var config = new SqlScaleoutConfiguration
            {
                ConnectionString = "Data Source=(local);Initial Catalog=SignalRSamples;Integrated Security=SSPI;MultipleActiveResultSets=true;Asynchronous Processing=True;",
                TableCount = 1,
            };

            return new SqlMessageBus(Resolver, config);
        }
    }
}
