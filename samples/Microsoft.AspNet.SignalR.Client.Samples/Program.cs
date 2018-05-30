// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Client.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            ExecuteAsync("http://192.168.170.108:5000/").Wait();
            // ExecuteAsync("http://localhost:8080/").Wait();
        }

        public static async Task<int> ExecuteAsync(string baseUrl)
        {
            var connection = new HubConnection(baseUrl);
            connection.TraceLevel = TraceLevels.All;
            connection.TraceWriter = Console.Out;

            var proxy = connection.CreateHubProxy("chat");

            proxy.On<string>("Send", Console.WriteLine);

            Console.CancelKeyPress += (sender, a) =>
            {
                a.Cancel = true;
                connection.Stop();
            };

            CancellationTokenSource closedTokenSource = null;

            connection.Closed += () =>
            {
                // This should never be null by the time this fires
                closedTokenSource.Cancel();

                Console.WriteLine("Connection closed...");
            };

            while (true)
            {
                // Dispose the previous token
                closedTokenSource?.Dispose();

                // Create a new token for this run
                closedTokenSource = new CancellationTokenSource();

                // Connect to the server
                if (!await ConnectAsync(connection))
                {
                    break;
                }

                Console.WriteLine("Connected to {0}", baseUrl); ;

                // Handle the connected connection
                while (true)
                {
                    try
                    {
                        var line = Console.ReadLine();

                        if (line == null || closedTokenSource.Token.IsCancellationRequested)
                        {
                            break;
                        }

                        await proxy.Invoke<object>("Send", line);
                    }
                    catch (IOException)
                    {
                        // Process being shutdown
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        // The connection closed
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        // We're shutting down the client
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Send could have failed because the connection closed
                        System.Console.WriteLine(ex);
                        break;
                    }
                }
            }

            return 0;
        }

        private static async Task<bool> ConnectAsync(HubConnection connection)
        {
            // Keep trying to until we can start
            while (true)
            {
                try
                {
                    await connection.Start();
                    return true;
                }
                catch (ObjectDisposedException)
                {
                    // Client side killed the connection
                    return false;
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to connect, trying again in 5000(ms)");

                    await Task.Delay(5000);
                }
            }
        }
    }
}
