// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;

namespace Microsoft.AspNet.SignalR.Crank
{
    class Program
    {
        private static volatile bool _running = true;

        static void Main(string[] args)
        {
            Console.WriteLine("Crank v{0}", typeof(Program).Assembly.GetName().Version);
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: crank [url] [numclients] <batchSize> <batchInterval>");
                return;
            }

            ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;

            string url = args[0];
            int clients = Int32.Parse(args[1]);
            int batchSize = args.Length < 3 ? 50 : Int32.Parse(args[2]);
            int batchInterval = args.Length < 4 ? 500 : Int32.Parse(args[3]);

            // Increase the number of min threads in the threadpool
            ThreadPool.SetMinThreads(clients, 2);

            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            var connections = new ConcurrentBag<Connection>();

            var totalRunStopwatch = Stopwatch.StartNew();

            Task.Run(async () =>
            {
                Console.WriteLine("Ramping up connections. Batch size {0}.", batchSize);

                var rampupStopwatch = Stopwatch.StartNew();
                await ConnectBatches(url, clients, batchSize, batchInterval, connections);

                Console.WriteLine("Started {0} connection(s).", connections.Count);
                Console.WriteLine("Setting up event handlers");

                rampupStopwatch.Stop();

                Console.WriteLine("Ramp up complete in {0}.", rampupStopwatch.Elapsed);

            });

            Console.WriteLine("Press 'q' to quit.");

            while (true)
            {
                var keyInfo = Console.ReadKey(intercept: true);

                if (keyInfo.Key == ConsoleKey.Q)
                {
                    break;
                }

                Console.WriteLine("Total Running time: {0}", totalRunStopwatch.Elapsed);
                Console.WriteLine("End point: {0}", url);
                Console.WriteLine("Total connections: {0}", clients);

                foreach (var g in connections.GroupBy(c => c.State))
                {
                    Console.WriteLine(g.Key + " connections: {0}", g.Count());
                }

                foreach (var g in connections.GroupBy(c => c.Transport.Name))
                {
                    Console.WriteLine(g.Key + " connections: {0}", g.Count());
                }
            }

            totalRunStopwatch.Stop();

            _running = false;

            Console.WriteLine("Closing connection(s).");
            Parallel.ForEach(connections, connection => connection.Stop());
        }

        private static async Task ConnectBatches(string url, int clients, int batchSize, int batchInterval, ConcurrentBag<Connection> connections)
        {
            var batchStopwatch = Stopwatch.StartNew();
            Console.WriteLine("Remaining clients {0}", clients);

            while (true)
            {
                int processed = Math.Min(clients, batchSize);

                await ConnectBatch(url, processed, connections);

                batchStopwatch.Stop();
                Console.WriteLine("Batch took {0}", batchStopwatch.Elapsed);

                int remaining = clients - processed;

                if (remaining <= 0)
                {
                    break;
                }

                clients = remaining;

                await Task.Delay(batchInterval);
            }
        }

        private static Task ConnectBatch(string url, int batchSize, ConcurrentBag<Connection> connections)
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
                    await connection.Start();
                    
                    if (_running)
                    {
                        connections.Add(connection);
                    }

                    var clientId = connection.ConnectionId;

                    //connection.Received += data =>
                    //{
                    //    Console.WriteLine("Client {0} RECEIVED: {1}", clientId, data);
                    //};

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
    }
}
