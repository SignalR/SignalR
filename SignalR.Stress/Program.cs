using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace SignalR.Stress
{
    class Program
    {
        private static Timer _rateTimer;
        private static bool _measuringRate;
        private static Stopwatch _sw = Stopwatch.StartNew();
        private static double _receivesPerSecond;
        private static double _peakReceivesPerSecond;
        private static double _avgReceivesPerSecond;
        private static long _received;
        private static long _avgLastReceivedCount;
        private static long _lastReceivedCount;
        private static DateTime _avgCalcStart;
        private static long _rate = 10;
        private static int _runs = 0;
        private static int _step = 10;
        private static int _stepInterval = 30;
        private static int _clients = 1000;
        private static Exception _exception;

        public static long TotalRate
        {
            get
            {
                return _rate * _clients;
            }
        }

        static void Main(string[] args)
        {
            var bus = new InProcessMessageBus();
            var eventKeys = new[] { "a", "b", "c" };
            string payload = GetPayload();

            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            for (int i = 0; i < _clients; i++)
            {
                Task.Factory.StartNew(() => StartClientLoop(bus, eventKeys), TaskCreationOptions.LongRunning);
            }

            Task.Factory.StartNew(() =>
            {
                while (_exception == null)
                {
                    long old = _rate;
                    var interval = TimeSpan.FromMilliseconds(1000.0 / _rate);
                    while (Interlocked.Read(ref _rate) == old && _exception == null)
                    {
                        try
                        {
                            bus.Send("a", payload).ContinueWith(task =>
                            {
                                Interlocked.Exchange(ref _exception, task.Exception);
                            },
                            TaskContinuationOptions.OnlyOnFaulted);

                            Thread.Sleep(interval);
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Exchange(ref _exception, ex);
                        }
                    }
                }
            },
            TaskCreationOptions.LongRunning);

            MeasureStats();

            Console.ReadLine();
        }

        private static string GetPayload(int n = 32)
        {
            return Encoding.UTF8.GetString(Enumerable.Range(0, n).Select(i => (byte)i).ToArray());
        }

        static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Interlocked.Exchange(ref _exception, e.Exception);
            e.SetObserved();
        }

        private static void StartClientLoop(InProcessMessageBus bus, string[] eventKeys)
        {
            ReceiveLoop(bus, eventKeys, null);
        }

        private static void ReceiveLoop(InProcessMessageBus bus, string[] eventKeys, ulong? id)
        {
            try
            {
                bus.GetMessagesSince(eventKeys, id).ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Interlocked.Exchange(ref _exception, task.Exception);
                    }
                    else
                    {
                        var list = task.Result;
                        id = list[list.Count - 1].Id;
                        Interlocked.Increment(ref _received);
                        Interlocked.Increment(ref _avgLastReceivedCount);

                        ReceiveLoop(bus, eventKeys, id);
                    }
                });
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref _exception, ex);
            }
        }

        public static void MeasureStats()
        {
            _sw.Start();
            _avgCalcStart = DateTime.UtcNow;
            var resultsPath = Guid.NewGuid().ToString() + ".csv";
            File.WriteAllText(resultsPath, "Target Rate, RPS, Peak RPS, Avg RPS\n");

            _rateTimer = new Timer(_ =>
            {
                if (_measuringRate)
                {
                    return;
                }
                _measuringRate = true;

                try
                {
                    var now = DateTime.UtcNow;
                    var timeDiffSecs = _sw.Elapsed.TotalSeconds;

                    _sw.Restart();

                    if (timeDiffSecs <= 0)
                    {
                        return;
                    }

                    if (_exception != null)
                    {
                        Console.WriteLine("Failed With:\r\n {0}", _exception.GetBaseException());
                        _rateTimer.Change(-1, -1);
                        _rateTimer.Dispose();
                        _rateTimer = null;
                        return;
                    }

                    var recv = Interlocked.Read(ref _received);
                    var recvDiff = recv - _lastReceivedCount;
                    var recvPerSec = recvDiff / timeDiffSecs;
                    _receivesPerSecond = recvPerSec;

                    _lastReceivedCount = recv;

                    Console.Clear();
                    Console.WriteLine("Total Rate: {0:0.000} (mps) = {1:0.000} (mps) * {2} (clients)", TotalRate, _rate, _clients);
                    var d1 = Math.Max(0, TotalRate - _receivesPerSecond);
                    Console.WriteLine("RPS: {0:0.000} (diff: {1:0.000}, {2:0.00}%)", Math.Min(TotalRate, _receivesPerSecond), d1, d1 * 100.0 / TotalRate);
                    var d2 = Math.Max(0, TotalRate - _peakReceivesPerSecond);
                    Console.WriteLine("Peak RPS: {0:0.000} (diff: {1:0.000} {2:0.00}%)", Math.Min(TotalRate, _peakReceivesPerSecond), d2, d2 * 100.0 / TotalRate);
                    var d3 = Math.Max(0, TotalRate - _avgReceivesPerSecond);
                    Console.WriteLine("Avg RPS: {0:0.000} (diff: {1:0.000} {2:0.00}%)", Math.Min(TotalRate, _avgReceivesPerSecond), d3, d3 * 100.0 / TotalRate);

                    File.AppendAllText(resultsPath, String.Format("{0}, {1}, {2}, {3}\n", TotalRate, _receivesPerSecond, _peakReceivesPerSecond, _avgReceivesPerSecond));


                    if (recvPerSec < long.MaxValue && recvPerSec > _peakReceivesPerSecond)
                    {
                        Interlocked.Exchange(ref _peakReceivesPerSecond, recvPerSec);
                    }

                    _avgReceivesPerSecond = _avgLastReceivedCount / (now - _avgCalcStart).TotalSeconds;

                    if (_runs > 0 && _runs % _stepInterval == 0)
                    {
                        _avgCalcStart = DateTime.UtcNow;
                        _avgLastReceivedCount = 0;
                        long old = _rate;
                        long @new = old + _step;
                        while (Interlocked.Exchange(ref _rate, @new) == old) { }
                    }

                    _runs++;

                }
                finally
                {
                    _measuringRate = false;
                }
            }, null, 1000, 1000);
        }
    }
}
