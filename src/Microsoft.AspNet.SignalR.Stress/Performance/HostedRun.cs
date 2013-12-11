// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;

namespace Microsoft.AspNet.SignalR.Stress.Performance
{
    /// <summary>
    /// Base class for the Host based run. including MemoryHost/External/IISExpress/OWINSelfHost
    /// </summary>
    public abstract class HostedRun : RunBase
    {
        private string _scenarioName; 

        public HostedRun(RunData runData)
            : base(runData)
        {
        }

        public ITestHost Host { get; set; }

        protected override string ScenarioName
        {
            get
            {
                if (_scenarioName == null)
                {
                    _scenarioName = base.ScenarioName + "-" + RunData.Host + "-" + RunData.Transport;
                }

                return _scenarioName;
            }
        }

        public override void Initialize()
        {
            // Create the host
            Host = HostedTestFactory.CreateHost(RunData.Host, RunData.Transport, ScenarioName, RunData.Url);
            Host.Resolver = Resolver;
            Host.Initialize();

            base.Initialize();
        }

        protected override void InitializePerformanceCounters()
        {
            if (RunData.Host != "External")
            {
                // No need to collect perf counters on the client side if servers are remote
                base.InitializePerformanceCounters();
            }
        }

        protected override IPerformanceCounter[] GetPerformanceCounters(IPerformanceCounterManager counterManager)
        {
            if (RunData.Host != "External")
            {
                return base.GetPerformanceCounters(counterManager);
            }

            return new IPerformanceCounter[0];
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
