using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class StartIISTask : Task
    {
        [Required]
        public ITaskItem[] HostLocation { get; set; }

        public override bool Execute()
        {
            try
            {
                var myHost = new IISExpressTestHost(HostLocation[0].ToString());

                myHost.Initialize(2, 120, 10, 1, false);
            }
            catch(Exception ex)
            {
                Log.LogError(ex.ToString());
                throw;
            }

            return true;
        }
    }
}
