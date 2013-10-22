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

        public void SendProgressAfterMethodReturn(IProgress<int> progress)
        {
            // Force the progress to be accessed *after* the hub method invocation has returned
            // by jumping threads and waiting for a short amount of time.
            Task.Run(async () =>
            {
                await Task.Delay(50);
                try
                {
                    progress.Report(100);
                    Clients.Caller.sendProgressAfterMethodReturnResult(false);
                }
                catch (InvalidOperationException)
                {
                    Clients.Caller.sendProgressAfterMethodReturnResult(true);
                }
            });
        }

        public void ReportProgressInt(IProgress<int> progress)
        {
            progress.Report(100);
        }

        public void ReportProgressString(IProgress<string> progress)
        {
            progress.Report("Progress is 100%");
        }

        public void ReportProgressTyped(IProgress<ProgressUpdate> progress)
        {
            progress.Report(new ProgressUpdate { Percent = 100, Message = "Progress is 100%" });
        }

        public void ReportProgressDynamic(IProgress<dynamic> progress)
        {
            progress.Report(new { Percent = 100, Message = "Progress is 100%" });
        }

        public class ProgressUpdate
        {
            public int Percent { get; set; }
            public string Message { get; set; }
        }
    }
}
