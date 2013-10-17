using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    [HubName("progress")]
    public class ProgressHub : Hub
    {
        public async Task<string> DoLongRunningJob(string jobName, IProgress<int> progress)
        {
            for (int i = 0; i <= 100; i += 10)
            {
                progress.Report(i);
            }

            await Task.Delay(100);

            return String.Format("{0} done!", jobName);
        }
    }
}
