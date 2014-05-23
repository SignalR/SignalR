using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private readonly Queue<string> _errorTextQueue;
        private readonly Queue<string> _outputTextQueue;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private Task runner;

        public ProcessRunner(string path, string arguments)
        {
            _startInfo = new ProcessStartInfo();
            _startInfo.FileName = path;
            _startInfo.Arguments = arguments;
            _startInfo.CreateNoWindow = true;
            _startInfo.UseShellExecute = false;
            _startInfo.RedirectStandardError = true;
            _startInfo.RedirectStandardOutput = true;
            _errorTextQueue = new Queue<string>();
            _outputTextQueue = new Queue<string>();
            _cancellationTokenSource = new CancellationTokenSource();
            Status = ProcessStatus.CREATED;
        }

        public int ProcessId { get; private set; }

        public ProcessStatus Status { get; private set; }

        public bool TryGetErrorText(out string text)
        {
            lock (_errorTextQueue)
            {
                if (_errorTextQueue.Count > 0)
                {
                    text = _errorTextQueue.Dequeue();
                    return true;
                }
                else
                {
                    text = "";
                    return false;
                }
            }
        }

        public bool TryGetOutputText(out string text)
        {
            lock (_outputTextQueue)
            {
                if (_outputTextQueue.Count > 0)
                {
                    text = _outputTextQueue.Dequeue();
                    return true;
                }
                else
                {
                    text = "";
                    return false;
                }
            }
        }

        public void Start()
        {
            if (Status != ProcessStatus.CREATED)
            {
                throw new InvalidOperationException();
            }
            Status = ProcessStatus.STARTING;
            runner = Run();
        }

        private async Task Run()
        {
            using (var process = Process.Start(_startInfo))
            {
                Status = ProcessStatus.RUNNING;
                ProcessId = process.Id;

                var errorReader = process.StandardError;
                var errorRead = errorReader.ReadLineAsync();

                var outputReader = process.StandardOutput;
                var outputRead = outputReader.ReadLineAsync();

                while (!process.HasExited)
                {
                    if (errorRead.IsCompleted)
                    {
                        lock (_errorTextQueue)
                        {
                            _errorTextQueue.Enqueue(errorRead.Result);
                        }
                        errorRead = errorReader.ReadLineAsync();
                    }
                    if (outputRead.IsCompleted)
                    {
                        lock (_outputTextQueue)
                        {
                            _outputTextQueue.Enqueue(outputRead.Result);
                        }
                        outputRead = outputReader.ReadLineAsync();
                    }
                    await Task.Delay(1000);
                }
                await errorRead;
                lock (_errorTextQueue)
                {
                    _errorTextQueue.Enqueue(errorRead.Result);
                }
                await outputRead;
                lock (_outputTextQueue)
                {
                    _outputTextQueue.Enqueue(outputRead.Result);
                }
                process.WaitForExit();
            }
            Status = ProcessStatus.STOPPED;
        }
    }
}
