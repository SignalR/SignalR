// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNet.SignalR.Client.Http;
using CmdLine;

namespace Microsoft.AspNet.SignalR.Crank
{

    class Program
    {
        private static volatile bool _running = true;
        private static readonly SemaphoreSlim _batchLock = new SemaphoreSlim(1);

        private static readonly string AllocatedBytesPerConnectionKey = "Allocated Bytes/Connection";
        private static PerformanceCounter _connectionsConnected;
        private static PerformanceCounter _allocBytesPerSecCrank;
        private static CounterSample[] _allocBytesPerSecCrankSamples = new CounterSample[2];
        private static List<long> _allocBytesPerConnCrank = new List<long>();
        private static List<long> _connectionsConnectedServer;
        private static int _lastConnectionsCount = 0;

        static void Main(string[] args)
        {
            var arguments = ParseArguments();

            ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;

            // Increase the number of min threads in the threadpool
            ThreadPool.SetMinThreads(arguments.NumClients, 2);

            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            var connections = new ConcurrentBag<Connection>();
            Stopwatch stopwatch = null;
            TimeSpan endTime = TimeSpan.MaxValue;
            TimeSpan timeoutTime = TimeSpan.FromSeconds(arguments.Timeout);

            InitializeCounters(arguments);
            Task.Run(async () =>
            {
                Console.WriteLine("Ramping up connections. Batch size {0}.", arguments.BatchSize);

                stopwatch = Stopwatch.StartNew();
                await ConnectBatches(arguments.Url, arguments.Transport, arguments.NumClients, arguments.BatchSize, arguments.BatchInterval, connections);

                var rampupElapsed = stopwatch.Elapsed;
                endTime = rampupElapsed.Add(TimeSpan.FromSeconds(arguments.Duration));
                Console.WriteLine("Ramp up complete in {0}.", rampupElapsed);

            });

            while (true)
            {
                Sample(arguments, connections);

                if (stopwatch != null)
                {
                    if ((stopwatch.Elapsed > endTime) || (stopwatch.Elapsed > timeoutTime))
                    {
                        _running = false;
                        break;
                    }

                    LogConnections(arguments, connections, stopwatch.Elapsed);
                }

                Thread.Sleep(arguments.BatchInterval);
            }

            stopwatch.Stop();

            Console.WriteLine("Total Running time: {0}", stopwatch.Elapsed);
            Record();

            Parallel.ForEach(connections, connection => connection.Stop());
        }

        private static void InitializeCounters(CrankArguments arguments)
        {
            _allocBytesPerSecCrank = new PerformanceCounter(".NET CLR Memory", "Allocated Bytes/sec", Process.GetCurrentProcess().ProcessName, readOnly:true);
            _allocBytesPerSecCrankSamples[1] = _allocBytesPerSecCrank.NextSample();

            var server = GetServerName(arguments.Url);
            if (!String.IsNullOrEmpty(server) && !String.IsNullOrEmpty(arguments.SiteName))
            {
                _connectionsConnected = new PerformanceCounter("SignalR", "Connections Connected", arguments.SiteName, machineName:server);
                _connectionsConnectedServer = new List<long>();
            }
        }

        private static void Sample(CrankArguments arguments, ConcurrentBag<Connection> connections)
        {
            _batchLock.Wait();
            try
            {
                if (connections.Count() > _lastConnectionsCount)
                {
                    var connectionsAdded = connections.Count() - _lastConnectionsCount;
                    _lastConnectionsCount = connections.Count();

                    _allocBytesPerSecCrankSamples[0] = _allocBytesPerSecCrankSamples[1];
                    _allocBytesPerSecCrankSamples[1] = _allocBytesPerSecCrank.NextSample();
                    var elapsed = new TimeSpan(_allocBytesPerSecCrankSamples[1].TimeStamp - _allocBytesPerSecCrankSamples[0].TimeStamp);
                    var bytesAllocated = CounterSample.Calculate(_allocBytesPerSecCrankSamples[0], _allocBytesPerSecCrankSamples[1]) * elapsed.TotalSeconds;
                    var value = (long)Math.Round(bytesAllocated / connectionsAdded);
                    _allocBytesPerConnCrank.Add(value);

                    long connectionsConnected = 0;
                    if (_connectionsConnected != null)
                    {
                        connectionsConnected = (long)Math.Round(_connectionsConnected.NextValue());
                        _connectionsConnectedServer.Add(connectionsConnected);
                    }
#if PERFRUN
                    Microsoft.VisualStudio.Diagnostics.Measurement.MeasurementBlock.Mark((ulong)value, String.Format("Crank-{0};{1}({2})", arguments.Transport,
                        AllocatedBytesPerConnectionKey, _allocBytesPerSecCrank.InstanceName));
                    if (connectionsConnected > 0)
                    {
                        Microsoft.VisualStudio.Diagnostics.Measurement.MeasurementBlock.Mark((ulong)connectionsConnected, String.Format("Crank-{0};{1}({2})", arguments.Transport,
                            "Connections Connected", _connectionsConnected.InstanceName));
                    }
#endif
                }
            }
            finally
            {
                _batchLock.Release();
            }
        }

        private static void Record()
        {
            _allocBytesPerConnCrank.Sort();
            var count = _allocBytesPerConnCrank.Count;
            var trim = (int)(count * 0.1);
            var trimmedValues = new long[count - 2 * trim];
            Array.Copy(_allocBytesPerConnCrank.ToArray(), trim, trimmedValues, 0, trimmedValues.Length);

            double median = trimmedValues[trimmedValues.Length / 2];
            if (trimmedValues.Length % 2 == 0)
            {
                median = median + trimmedValues[(trimmedValues.Length / 2) - 1] / 2;
            }
            var average = trimmedValues.Average();
            var sumOfSquaresDiffs = trimmedValues.Select(v => (v - average) * (v - average)).Sum();
            var stdDevP = Math.Sqrt(sumOfSquaresDiffs / trimmedValues.Length) / average * 100;

            Console.WriteLine("{0}({1}) (MEDIAN):  {2}", AllocatedBytesPerConnectionKey, _allocBytesPerSecCrank.InstanceName, Math.Round(median));
            Console.WriteLine("{0}({1}) (AVERAGE): {2}", AllocatedBytesPerConnectionKey, _allocBytesPerSecCrank.InstanceName, Math.Round(average));
            Console.WriteLine("{0}({1}) (STDDEV%): {2}%", AllocatedBytesPerConnectionKey, _allocBytesPerSecCrank.InstanceName, Math.Round(stdDevP));

            if (_connectionsConnected != null)
            {
                Console.WriteLine("Max Connections Connected ({0}): {1}", _connectionsConnected.InstanceName, _connectionsConnectedServer.Max());
            }
        }

        private static string GetServerName(string url)
        {
            var match = Regex.Match(url, @"^\w+://([^/]+):\d+?/");
            if (match.Success && match.Groups.Count >= 2)
            {
                return match.Groups[1].Value;
            }
            return null;
        }

        private static void LogConnections(CrankArguments arguments, ConcurrentBag<Connection> connections, TimeSpan elapsed)
        {
            if (connections.Count == 0)
            {
                return;
            }

            var connected = connections.Where(c => c.State == ConnectionState.Connected).Count();
#if PERFRUN
            Microsoft.VisualStudio.Diagnostics.Measurement.MeasurementBlock.Mark((ulong)connected, String.Format("Crank-{0};Connections Connected", arguments.Transport));
#endif
            if (connections.First().Transport.GetType() == typeof(AutoTransport))
            {
                Console.WriteLine("[{0}] Connections: {1}/{2}, State={3}|{4}c|{5}r|{6}d, Transport={7}ws|{8}ss|{9}lp",
                    elapsed,
                    connections.Count(), arguments.NumClients,
                    connections.Where(c => c.State == ConnectionState.Connecting).Count(),
                    connected,
                    connections.Where(c => c.State == ConnectionState.Reconnecting).Count(),
                    connections.Where(c => c.State == ConnectionState.Disconnected).Count(),
                    connections.Where(c => c.Transport.Name.Equals("webSockets", StringComparison.InvariantCultureIgnoreCase)).Count(),
                    connections.Where(c => c.Transport.Name.Equals("serverSentEvents", StringComparison.InvariantCultureIgnoreCase)).Count(),
                    connections.Where(c => c.Transport.Name.Equals("longPolling", StringComparison.InvariantCultureIgnoreCase)).Count());
            }
            else
            {
                Console.WriteLine("[{0}] Connections: {1}/{2}, State={3}|{4}c|{5}r|{6}d",
                    elapsed,
                    connections.Count(), arguments.NumClients,
                    connections.Where(c => c.State == ConnectionState.Connecting).Count(),
                    connections.Where(c => c.State == ConnectionState.Connected).Count(),
                    connections.Where(c => c.State == ConnectionState.Reconnecting).Count(),
                    connections.Where(c => c.State == ConnectionState.Disconnected).Count());
            }
        }

        private static async Task ConnectBatches(string url, string transport, int clients, int batchSize, int batchInterval, ConcurrentBag<Connection> connections)
        {
            while (true)
            {
                int processed = Math.Min(clients, batchSize);

                await _batchLock.WaitAsync();
                try
                {
                    await ConnectBatch(url, transport, processed, connections);
                }
                finally
                {
                    _batchLock.Release();
                }

                int remaining = clients - processed;

                if (remaining <= 0)
                {
                    break;
                }

                clients = remaining;

                await Task.Delay(batchInterval);
            }
        }

        private static Task ConnectBatch(string url, string transport, int batchSize, ConcurrentBag<Connection> connections)
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = batchSize
            };

            var batchTcs = new TaskCompletionSource<object>();

            long remaining = batchSize;
            Parallel.For(0, batchSize, options, async i =>
            {
                var connection = new Connection(url);

                if (!_running)
                {
                    batchTcs.TrySetResult(null);
                    return;
                }

                try
                {
                    var clientTransport = GetTransport(transport);
                    await (clientTransport == null ? connection.Start() : connection.Start(clientTransport));
                    
                    if (_running)
                    {
                        connections.Add(connection);

                        var clientId = connection.ConnectionId;

                        connection.Error += e =>
                        {
                            Debug.WriteLine(String.Format("SIGNALR: Client {0} ERROR: {1}", clientId, e));
                        };

                        connection.Closed += () =>
                        {
                            Debug.WriteLine(String.Format("SIGNALR: Client {0} CLOSED", clientId));

                            // Remove it from the list on close
                            connections.TryTake(out connection);
                        };
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to start client. {0}", ex.GetBaseException());
                }
                finally
                {
                    if (Interlocked.Decrement(ref remaining) == 0)
                    {
                        // When all connections are connected, mark the task as complete
                        batchTcs.TrySetResult(null);
                    }
                }
            });

            return batchTcs.Task;
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.WriteLine(e.Exception.GetBaseException());
            e.SetObserved();
        }

        private static IClientTransport GetTransport(string transport)
        {
            if (!String.IsNullOrEmpty(transport))
            {
                var httpClient = new DefaultHttpClient();
                if (transport.Equals("WebSockets", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new WebSocketTransport(httpClient);
                }
                else if (transport.Equals("ServerSentEvents", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new ServerSentEventsTransport(httpClient);
                }
                else if (transport.Equals("LongPolling", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new LongPollingTransport(httpClient);
                }
                else if (transport.Equals("Auto", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new AutoTransport(httpClient);
                }
            }
            return null;
        }

        private static CrankArguments ParseArguments()
        {
            CrankArguments args = null;
            try
            {
                args = CommandLine.Parse<CrankArguments>();
            }
            catch (CommandLineException e)
            {
                Console.WriteLine(e.ArgumentHelp.Message);
                Console.WriteLine(e.ArgumentHelp.GetHelpText(Console.BufferWidth));
                Environment.Exit(1);
            }
            return args;
        }

        [CommandLineArguments(Program = "Crank")]
        private class CrankArguments
        {
            [CommandLineParameter(Command = "?", Name = "Help", Default = false, Description = "Show Help", IsHelp = true)]
            public bool Help { get; set; }

            [CommandLineParameter(Command = "Url", Required = true, Description = "URL for SignalR connections.")]
            public string Url { get; set; }

            [CommandLineParameter(Command = "Clients", Required = true, Description = "Number of clients.")]
            public int NumClients { get; set; }

            [CommandLineParameter(Command = "BatchSize", Required = false, Default = 50, Description = "Batch size for adding connections. Default: 50")]
            public int BatchSize { get; set; }

            [CommandLineParameter(Command = "BatchInterval", Required = false, Default = 500, Description = "Batch interval in milliseconds for adding connections. Default: 500")]
            public int BatchInterval { get; set; }

            [CommandLineParameter(Command = "Transport", Required = false, Default = "auto", Description = "Transport name. Default: auto")]
            public string Transport { get; set; }

            [CommandLineParameter(Command = "Duration", Required = false, Default = 30, Description = "Duration in seconds to persist connections after warmup completes. Default: 30")]
            public int Duration { get; set; }

            [CommandLineParameter(Command = "Timeout", Required = false, Default = 300, Description = "Timeout in seconds. Default: 300")]
            public int Timeout { get; set; }

            [CommandLineParameter(Command = "SiteName", Required = false, Default = "", Description = "Site name, used as instance for SignalR counters. Defaults to not collecting server data.")]
            public string SiteName { get; set; }
        }

    }
}
