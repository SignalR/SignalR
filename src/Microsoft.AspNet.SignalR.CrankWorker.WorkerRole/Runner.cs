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
                        string arguments = String.Format(argumentString, host, index);
                        arguments += " /TestManagerUrl:" + managerUrl;
                        arguments += " /TestManagerGuid:" + connection.ConnectionId;

                        var processRunner = new ProcessRunner(path, arguments);
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
            hub.Invoke("join", connection.ConnectionId).Wait();

            while (true)
            {
                hub.Invoke("addUpdateWorker", connection.ConnectionId, host, _status.ToString()).Wait();
                lock (_processRunners)
                {
                    foreach (var processRunner in _processRunners)
                    {
                        string errorText;
                        while (processRunner.TryGetErrorText(out errorText))
                        {
                            if ((errorText != null) && (errorText.Length > 0))
                            {
                                hub.Invoke("addTrace", host, errorText);
                            }
                        }
                    }
                }
                Task.Delay(1000).Wait();
            }
        }
    }
}
