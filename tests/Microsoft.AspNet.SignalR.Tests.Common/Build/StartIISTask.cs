using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class StartIISTask : Task
    {
        [Required]
        public ITaskItem[] HostLocation { get; set; }

        public override bool Execute()
        {
            var myHost = new IISExpressTestHost(HostLocation[0].ToString());

            myHost.Initialize(15, 120, 10, 1, false);

            return true;
        }
    }
}
