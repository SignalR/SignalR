using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.CrankWorker.WorkerRole
{
    public class ProcessRunner
    {
        public enum ProcessStatus
        {
            CREATED,
            STARTING,
            RUNNING,
            STOPPING,
            STOPPED
        };

        private readonly ProcessStartInfo _startInfo;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private Task runner;

        public ProcessRunner(ProcessStartInfo startInfo)
        {
            _startInfo = startInfo;
            _cancellationTokenSource = new CancellationTokenSource();
            Status = ProcessStatus.CREATED;
        }

        public int ProcessId { get; private set; }

        public ProcessStatus Status { get; private set; }

        public void Start()
        {
            if (Status != ProcessStatus.CREATED)
            {
                throw new InvalidOperationException();
            }
            Status = ProcessStatus.STARTING;
            runner = Run();
        }

        public async Task Stop()
        {
            if (Status != ProcessStatus.RUNNING)
            {
                throw new InvalidOperationException();
            }
            Status = ProcessStatus.STOPPING;
            _cancellationTokenSource.Cancel();
            await runner;
        }

        private async Task Run()
        {
            using (var process = Process.Start(_startInfo))
            {
                Status = ProcessStatus.RUNNING;
                while (!_cancellationTokenSource.IsCancellationRequested && !process.HasExited)
                {
                    await Task.Delay(1000);
                }
                if (!process.HasExited)
                {
                    process.Kill();
                }
                process.WaitForExit();
            }
            Status = ProcessStatus.STOPPED;
        }
    }
}
