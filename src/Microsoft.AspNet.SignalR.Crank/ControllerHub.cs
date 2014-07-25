// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin.Hosting;
using Owin;

namespace Microsoft.AspNet.SignalR.Crank
{
    public class ControllerHub : Hub
    {
        private static CrankArguments Arguments;
        private static IDisposable AppHost = null;
        private static int ClientsConnected;
        private static PerformanceCounters PerformanceCounters;
        private static List<ConnectionsSample> Samples = new List<ConnectionsSample>();
        private static int NextSample = 0;
        private static object FlushLock = new object();
        private static ControllerEvents TestPhase = ControllerEvents.None;
        private static Stopwatch TestTimer;
        private static IHubContext HubContext;

        public override Task OnConnected()
        {
            ClientsConnected++;
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            ClientsConnected--;
            return base.OnDisconnected(stopCalled);
        }

        public void Mark(int sampleId, int[] states)
        {
            MarkInternal(sampleId, states);
        }

        internal static void MarkInternal(int sampleId, int[] states)
        {
            Debug.Assert(sampleId < Samples.Count);
            Samples[sampleId].Add(states);
            FlushLog();
        }

        internal static void Start(CrankArguments arguments)
        {
            ControllerHub.Arguments = arguments;
            PerformanceCounters = new PerformanceCounters(new Uri(arguments.Url).Host, arguments.SignalRInstance);

            ThreadPool.QueueUserWorkItem(_ => Run());
        }

        private static void Run()
        {
            if (Arguments.NumClients > 1)
            {
                AppHost = WebApp.Start<Startup>(Arguments.ControllerUrl);

                if (!WaitForClientsToConnect())
                {
                    SignalPhaseChange(ControllerEvents.Abort);
                    for (int attempts = 0; (ClientsConnected > 0) && (attempts < CrankArguments.ConnectionPollAttempts); attempts++)
                    {
                        Thread.Sleep(CrankArguments.ConnectionPollIntervalMS);
                    }

                    AppHost.Dispose();
                    return;
                }
            }

            RunConnect();
            RunSend();
            RunDisconnect();

            WaitForLastSamples();

            if (AppHost != null)
            {
                AppHost.Dispose();
            }

            FlushLog(force:true);
        }

        private static void RunConnect()
        {
            InitializeLog();
            SignalPhaseChange(ControllerEvents.Connect);
            StartSampleLoop();

            BlockWhilePhase(ControllerEvents.Connect, breakCondition: () =>
            {
                if (Samples.Count == 0)
                {
                    return false;
                }
                if (TestTimer.Elapsed >= TimeSpan.FromSeconds(Arguments.ConnectTimeout))
                {
                    return true;
                }
                var lastSample = Samples.Last();
                if (lastSample.ServerAvailableMBytes > 0)
                {
                    if (lastSample.ServerAvailableMBytes <= Arguments.MinServerMBytes)
                    {
                        return true;
                    }
                }
                var connections = lastSample.Connected + lastSample.Reconnected;
                return connections >= Arguments.Connections;
            });

            SignalPhaseChange(ControllerEvents.Send);
        }

        private static void RunSend()
        {
            var timeout = TestTimer.Elapsed.Add(TimeSpan.FromSeconds(Arguments.SendTimeout));
            
            BlockWhilePhase(ControllerEvents.Send, breakCondition: () =>
            {
                return TestTimer.Elapsed >= timeout;
            });

            SignalPhaseChange(ControllerEvents.Disconnect);
        }

        private static void RunDisconnect()
        {
            BlockWhilePhase(ControllerEvents.Disconnect, breakCondition: () =>
            {
                if (TestTimer.Elapsed >= TimeSpan.FromSeconds(Arguments.ConnectTimeout))
                {
                    return true;
                }

                var lastSample = Samples.Last();
                var connections = lastSample.Connected + lastSample.Reconnected;
                return connections == 0;
            });

            SignalPhaseChange(ControllerEvents.Complete);
        }

        private static void StartSampleLoop()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                TestTimer = Stopwatch.StartNew();

                while ((TestPhase != ControllerEvents.Abort) && (TestPhase != ControllerEvents.Complete))
                {
                    SignalSample(TestTimer.Elapsed);
                    Thread.Sleep(Arguments.SampleInterval);
                }
            });
        }

        private static void InitializeLog()
        {
            File.WriteAllText(Arguments.LogFile, Environment.CommandLine + Environment.NewLine);
            File.WriteAllText(Arguments.LogFile, "TestPhase,Elapsed,Connected,Reconnected,Disconnected,ServerAvailableMBytes,ServerTcpConnectionsEst" + Environment.NewLine);
        }

        private static void FlushLog(bool force = false)
        {
            if (Samples.Count == 0)
            {
                return;
            }

            var expectedSampleCount = GetExpectedSampleCount();
            if ((NextSample < Samples.Count) && (Samples[NextSample].Count >= expectedSampleCount))
            {
                lock (FlushLock)
                {
                    for (; NextSample < Samples.Count; NextSample++)
                    {
                        var sample = Samples[NextSample];
                        if ((sample.Count < expectedSampleCount) && !force)
                        {
                            break;
                        }
                        var args = new object[] { sample.TestPhase, sample.TimeStamp, sample.Connected, sample.Reconnected, sample.Disconnected, sample.ServerAvailableMBytes, sample.ServerTcpConnectionsEst };
                        Console.WriteLine("{1} ({0}): {2} Connected, {3} Reconnected, {4} Disconnected, {5} AvailMBytes, {6} TcpConnEst", args);
                        File.AppendAllText(Arguments.LogFile, String.Format("{0},{1},{2},{3},{4},{5},{6}", args) + Environment.NewLine);
                    }
                }
            }
        }

        private static bool WaitForClientsToConnect()
        {
            Console.WriteLine("Waiting on Clients...");
            
            int attempts = 0;

            while (ClientsConnected < Arguments.NumClients)
            {
                Thread.Sleep(CrankArguments.ConnectionPollIntervalMS);

                if (++attempts > CrankArguments.ConnectionPollAttempts)
                {
                    Console.WriteLine("Aborting: Clients did not connect in time.");
                    return false;
                }
            }

            return true;
        }

        private static int GetExpectedSampleCount()
        {
            return PerformanceCounters.SignalRCountersAvailable ? 1 : Arguments.NumClients;
        }

        private static void WaitForLastSamples()
        {
            var attempts = 0;
            var expectedSampleCount = GetExpectedSampleCount();

            while (Samples.Last().Count < expectedSampleCount)
            {
                if (++attempts > CrankArguments.ConnectionPollAttempts)
                {
                    break;
                }

                Thread.Sleep(CrankArguments.ConnectionPollIntervalMS);
            }
        }

        private static void BlockWhilePhase(ControllerEvents phase, Func<bool> breakCondition = null)
        {
            while (TestPhase == phase)
            {
                if ((breakCondition != null) && breakCondition())
                {
                    break;
                }

                Thread.Sleep(CrankArguments.ConnectionPollIntervalMS);
            }
        }

        private static void SignalPhaseChange(ControllerEvents phase)
        {
            if (phase != ControllerEvents.Abort)
            {
                TestPhase = phase;
            }

            if (AppHost == null)
            {
                Client.OnPhaseChanged(phase);
            }
            else
            {
                BroadcastEvent(phase);
            }
        }

        private static void SignalSample(TimeSpan timestamp)
        {
            Samples.Add(new ConnectionsSample(Enum.GetName(typeof(ControllerEvents), TestPhase), timestamp, PerformanceCounters.ServerAvailableMBytes, PerformanceCounters.ServerTcpConnectionsEst));

            if (PerformanceCounters.SignalRCountersAvailable)
            {
                ControllerHub.MarkInternal(Samples.Count - 1, new int[] {
                    PerformanceCounters.SignalRConnectionsCurrent,
                    PerformanceCounters.SignalRConnectionsReconnected,
                    PerformanceCounters.SignalRConnectionsDisconnected
                });
            }
            else
            {
                // Use client connection states
                if (AppHost == null)
                {
                    Client.OnSample(Samples.Count - 1);
                }
                else
                {
                    BroadcastEvent(ControllerEvents.Sample, Samples.Count - 1);
                }
            }
        }

        private static void BroadcastEvent(ControllerEvents controllerEvent, int id = 0)
        {
            if (HubContext == null)
            {
                HubContext = GlobalHost.ConnectionManager.GetHubContext<ControllerHub>();
            }

            HubContext.Clients.All.broadcast(controllerEvent, id);
        }

        public class Startup
        {
            public void Configuration(IAppBuilder app)
            {
                app.MapSignalR();
            }
        }
    }
}
