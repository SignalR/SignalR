using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.CrankWorker.WorkerRole
{
    public class Runner
    {
        private enum ClientStatus
        {
            CREATED,
            READY,
            SPAWNING,
            KILLING,
            ERROR
        };

        private volatile ClientStatus _status;
        private readonly IList<ProcessRunner> _processRunners;

        public Runner()
        {
            _status = ClientStatus.READY;
            _processRunners = new List<ProcessRunner>();
        }

        public void Run(string managerUrl, string path)
        {
            var host = Dns.GetHostName();
            var connection = new HubConnection(managerUrl);
            var hub = connection.CreateHubProxy("TestManagerHub");

            hub.On<int, string>("startProcesses", async (instances, argumentString) =>
            {
                string errorMessage = "";
                try
                {
                    for (int index = 0; index < instances; index++)
                    {
                        _status = ClientStatus.SPAWNING;
                        var startInfo = new ProcessStartInfo();
                        startInfo.CreateNoWindow = true;
                        startInfo.UseShellExecute = false;
                        startInfo.FileName = path;
                        startInfo.Arguments = String.Format(argumentString, host, index);

                        var processRunner = new ProcessRunner(startInfo);
                        lock (_processRunners)
                        {
                            _processRunners.Add(processRunner);
                        }
                        processRunner.Start();
                        _status = ClientStatus.READY;
                    }
                }
                catch (Exception exception)
                {
                    errorMessage = exception.ToString();
                    _status = ClientStatus.ERROR;
                }

                if (_status == ClientStatus.ERROR)
                {
                    await hub.Invoke("addTrace", host, errorMessage);
                }

            });

            hub.On<int>("stopProcess", async (processId) =>
            {
                string errorMessage = "";
                try
                {
                    ProcessRunner processRunner;
                    lock (_processRunners)
                    {
                        processRunner = _processRunners[processId];
                    }
                    _status = ClientStatus.KILLING;
                    await processRunner.Stop();
                    _status = ClientStatus.READY;
                }
                catch (Exception exception)
                {
                    errorMessage = exception.ToString();
                    _status = ClientStatus.ERROR;
                }

                if (_status == ClientStatus.ERROR)
                {
                    await hub.Invoke("addTrace", host, errorMessage);
                }
                await hub.Invoke("removeProcess", processId);
            });

            while (connection.State == ConnectionState.Disconnected)
            {
                try
                {
                    connection.Start().Wait();
                    _status = ClientStatus.READY;
                }
                catch (Exception) { }
            }

            while (connection.State != ConnectionState.Connected) ;
            hub.Invoke("joinConnectionGroup").Wait();

            while (true)
            {
                hub.Invoke("addUpdateWorker", host, _status.ToString()).Wait();
                lock (_processRunners)
                {
                    for (int index = 0; index < _processRunners.Count; index++)
                    {
                        var status = _processRunners[index].Status;
                        if (status != ProcessRunner.ProcessStatus.STOPPED)
                        {
                            hub.Invoke("addUpdateProcess", index, status.ToString());
                        }
                    }
                }
                Task.Delay(1000).Wait();
            }
        }
    }
}
