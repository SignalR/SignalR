using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;

namespace Microsoft.AspNet.SignalR.Crank
{
    public class Client
    {
        private static CrankArguments Arguments;
        private static ConcurrentBag<Connection> Connections = new ConcurrentBag<Connection>();
        private static ConcurrentBag<IHubProxy> HubProxies = new ConcurrentBag<IHubProxy>();
        private static HubConnection ControllerConnection;
        private static IHubProxy ControllerProxy;
        private static ControllerEvents TestPhase = ControllerEvents.None;

        public static void Main()
        {
            Arguments = CrankArguments.Parse();

            ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
            ThreadPool.SetMinThreads(Arguments.Connections, 2);
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            if (Arguments.IsController)
            {
                ControllerHub.Start(Arguments);
            }

            Run();
        }

        private static void Run()
        {
            var remoteController = !Arguments.IsController || (Arguments.NumClients > 1);
            if (remoteController)
            {
                OpenControllerConnection();
            }

            if (!Arguments.IsController)
            {
                Console.WriteLine("Waiting on Controller...");
            }
            while (TestPhase != ControllerEvents.Connect)
            {
                if (TestPhase == ControllerEvents.Abort)
                {
                    Console.WriteLine("Test Aborted");
                    return;
                }
                Thread.Sleep(CrankArguments.ConnectionPollIntervalMS);
            }

            RunConnect();
            RunSend();
            RunDisconnect();

            if (remoteController)
            {
                CloseControllerConnection();
            }
        }

        private static void OpenControllerConnection()
        {
            ControllerConnection = new HubConnection(Arguments.ControllerUrl);
            ControllerProxy = ControllerConnection.CreateHubProxy("ControllerHub");

            ControllerProxy.On<ControllerEvents, int>("broadcast", (controllerEvent, id) =>
            {
                if (controllerEvent == ControllerEvents.Sample)
                {
                    OnSample(id);
                }
                else
                {
                    OnPhaseChanged(controllerEvent);
                }
            });

            int attempts = 0;
            while (true)
            {
                try
                {
                    ControllerConnection.Start().Wait();
                    break;
                }
                catch (Exception)
                {
                    attempts++;
                    if (attempts > CrankArguments.ConnectionPollAttempts)
                    {
                        throw new InvalidOperationException("Failed to connect to the controller hub");
                    }
                    Thread.Sleep(CrankArguments.ConnectionPollIntervalMS);
                }
            }
        }

        internal static void CloseControllerConnection()
        {
            if (ControllerConnection != null)
            {
                ControllerConnection.Stop();
                ControllerConnection = null;
                ControllerProxy = null;
            }
        }

        internal static void OnPhaseChanged(ControllerEvents phase)
        {
            Debug.Assert(phase != ControllerEvents.None);
            Debug.Assert(phase != ControllerEvents.Sample);
            TestPhase = phase;
            if (!Arguments.IsController)
            {
                Console.WriteLine("Running: {0}", Enum.GetName(typeof(ControllerEvents), phase));
            }
        }

        internal static void OnSample(int id)
        {
            var states = Connections.Select(c => c.State);
            var statesArr = new int[3]
            {
                states.Where(s => s == ConnectionState.Connected).Count(),
                states.Where(s => s == ConnectionState.Reconnecting).Count(),
                states.Where(s => s == ConnectionState.Disconnected).Count()
            };
            if (ControllerProxy != null)
            {
                ControllerProxy.Invoke("Mark", id, statesArr);
            }
            else
            {
                ControllerHub.MarkInternal(id, statesArr);
            }
            if (!Arguments.IsController)
            {
                Console.WriteLine("{0} Connected, {1} Reconnected, {2} Disconnected", statesArr[0], statesArr[1], statesArr[2]);
            }
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.WriteLine(e.Exception.GetBaseException());
            e.SetObserved();
        }

        private static void RunSend()
        {
            var payload = (Arguments.SendBytes == 0) ? String.Empty : new string('a', Arguments.SendBytes);

            while (TestPhase == ControllerEvents.Send)
            {
                if (!String.IsNullOrEmpty(payload))
                {
                    var send = Parallel.ForEach(Connections, c => c.Send(payload));
                    while (!send.IsCompleted)
                    {
                        Thread.Sleep(250);
                    }
                }
                Thread.Sleep(Arguments.SendInterval);
            }
        }

        private static void RunDisconnect()
        {
            if (Connections.Count > 0)
            {
                if ((TestPhase == ControllerEvents.Disconnect) || (TestPhase == ControllerEvents.Abort))
                {
                    var dispose = Parallel.ForEach(Connections, c => c.Dispose());
                    while (!dispose.IsCompleted)
                    {
                        Thread.Sleep(CrankArguments.ConnectionPollIntervalMS);
                    }
                }
            }
        }

        private static void RunConnect()
        {
            var batched = Arguments.BatchSize > 1;
            while (TestPhase == ControllerEvents.Connect)
            {
                if (batched)
                {
                    ConnectBatch();
                }
                else
                {
                    ConnectSingle();
                }
                if (Arguments.ConnectInterval > 0)
                {
                    Thread.Sleep(Arguments.ConnectInterval);
                }
            }
        }

        private static void ConnectBatch()
        {
            var batch = Parallel.For(0, Arguments.BatchSize, new ParallelOptions { MaxDegreeOfParallelism = Arguments.BatchSize }, i =>
            {
                ConnectSingle();
            });
            while (!batch.IsCompleted)
            {
                Thread.Sleep(250);
            }
        }

        private static void ConnectSingle()
        {
            string failReason = "Unknown";
            var connection = CreateConnection();
            for (int attempts = 0; attempts < CrankArguments.ConnectionPollAttempts; attempts++)
            {
                try
                {
                    if (Arguments.Transport == null)
                    {
                        connection.Start().Wait();
                    }
                    else
                    {
                        connection.Start(Arguments.Transport).Wait();
                    }
                    break;
                }
                catch (Exception e)
                {
                    failReason = String.Format("{0}: {1}", e.GetType(), e.Message);
                }
            }
            if (connection.State == ConnectionState.Connected)
            {
                connection.Closed += () =>
                {
                    Connections.TryTake(out connection);
                };
                Connections.Add(connection);
            }
            else
            {
                Console.WriteLine("Connection.Start Failed: " + failReason);
                connection.Dispose();
            }
        }

        private static Connection CreateConnection()
        {
            return new Connection(Arguments.Url);
        }
    }
}
