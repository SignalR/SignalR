// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;

namespace Microsoft.AspNet.SignalR.Stress.Stress
{
    public abstract class HostedStressRun : StressRunBase
    {
        public HostedStressRun(RunData runData)
            : base(runData)
        {
            ScenarioName = base.GetContractName() + "-" + RunData.Host + "-" + RunData.Transport;
        }

        public ITestHost Host { get; set; }

        protected string ScenarioName { get; set; }

        public override void Initialize()
        {
            // Create the host
            Host = HostedTestFactory.CreateHost(RunData.Host, RunData.Transport, ScenarioName, RunData.Url);
            Host.Resolver = Resolver;
            Host.Initialize();

            // Create the client 
            base.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            // dispose the host
            if (Host != null && disposing)
            {
                Host.Dispose();
                Host = null;
            }
        }
    }
}
