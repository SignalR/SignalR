using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        private static double _sendsPerSecond;
        private static double _peakSendsPerSecond;
        private static double _avgSendsPerSecond;
        private static long _sent;
        private static long _avgLastSendsCount;
        private static long _lastSendsCount;

        private static DateTime _avgCalcStart;
        private static long _lastSendTimeTicks;
        private static long _rate = 1;
        private static int _runs = 0;
        private static int _step = 1;
        private static int _stepInterval = 50;
        private static int _clients = 1000;
        private static int _clientsRunning = 0;
        private static int _senders = 1;
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
            var resolver = new DefaultDependencyResolver();
            var bus = new MessageBus(resolver);            
            string payload = GetPayload();

            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            for (int i = 0; i < _clients; i++)
            {
                var subscriber = new Subscriber(new[] { "a", "b", "c" });
                Task.Factory.StartNew(() => StartClientLoop(bus, subscriber), TaskCreationOptions.LongRunning);
                //ThreadPool.QueueUserWorkItem(_ => StartClientLoop(bus, eventKeys));
                //(new Thread(_ => StartClientLoop(bus, eventKeys))).Start();
            }

            for (var i = 1; i <= _senders; i++)
            {
                //ThreadPool.QueueUserWorkItem(_ => StartSendLoop(bus, payload));
                Task.Factory.StartNew(() => StartSendLoop(i, bus, payload), TaskCreationOptions.LongRunning);
            }

            MeasureStats();

            Console.ReadLine();
        }

        private static void StartSendLoop(int clientId, MessageBus bus, string payload)
        {
            while (_exception == null)
            {
                long old = _rate;
                var interval = TimeSpan.FromMilliseconds((1000.0 / _rate) * _senders);
                //var interval = TimeSpan.FromMilliseconds(1000.0 / _rate);
                while (Interlocked.Read(ref _rate) == old && _exception == null)
                {
                    try
                    {
                        var sw = Stopwatch.StartNew();
                        bus.Publish(clientId.ToString(), "a", payload);
                        sw.Stop();
                        Interlocked.Exchange(ref _lastSendTimeTicks, sw.ElapsedTicks);

                        Interlocked.Increment(ref _sent);
                        Interlocked.Increment(ref _avgLastSendsCount);

                        Thread.Sleep(interval);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Exchange(ref _exception, ex);
                    }
                }
            }
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

        private static void StartClientLoop(MessageBus bus, ISubscriber subscriber)
        {
            Interlocked.Increment(ref _clientsRunning);
            ReceiveLoop(bus, subscriber);
        }

        private static void ReceiveLoop(MessageBus bus, ISubscriber subscriber)
        {
            try
            {
                bus.Subscribe(subscriber, null, (ex, result) =>
                {
                    if (ex != null)
                    {
                        Interlocked.Exchange(ref _exception, ex);
                    }
                    else
                    {
                        Interlocked.Increment(ref _received);
                        Interlocked.Increment(ref _avgLastReceivedCount);
                    }

                    return TaskAsyncHelper.Empty;
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

                    Console.Clear();
                    Console.WriteLine("Started {0} of {1} clients", _clientsRunning, _clients);
                    //Console.WriteLine("Last time to send: {0}ms", TimeSpan.FromTicks(Interlocked.Read(ref _lastSendTimeTicks)).TotalMilliseconds);

                    Console.WriteLine("Total Rate: {0} (mps) = {1} (mps) * {2} (clients)", TotalRate, _rate, _clients);
                    Console.WriteLine();

                    // Sends
                    var sends = Interlocked.Read(ref _sent);
                    var sendsDiff = sends - _lastSendsCount;
                    var sendsPerSec = sendsDiff / timeDiffSecs;
                    _sendsPerSecond = sendsPerSec;

                    _lastSendsCount = sends;

                    Console.WriteLine("----- SENDS -----");

                    var s1 = Math.Max(0, _rate - _sendsPerSecond);
                    Console.WriteLine("SPS: {0:0.000} (diff: {1:0.000}, {2:0.00}%)", _sendsPerSecond, s1, s1 * 100.0 / _rate);
                    var s2 = Math.Max(0, _rate - _peakSendsPerSecond);
                    Console.WriteLine("Peak SPS: {0:0.000} (diff: {1:0.000} {2:0.00}%)", _peakSendsPerSecond, s2, s2 * 100.0 / _rate);
                    var s3 = Math.Max(0, _rate - _avgSendsPerSecond);
                    Console.WriteLine("Avg SPS: {0:0.000} (diff: {1:0.000} {2:0.00}%)", _avgSendsPerSecond, s3, s3 * 100.0 / _rate);
                    Console.WriteLine();

                    if (sendsPerSec < long.MaxValue && sendsPerSec > _peakSendsPerSecond)
                    {
                        Interlocked.Exchange(ref _peakSendsPerSecond, sendsPerSec);
                    }

                    _avgSendsPerSecond = _avgLastSendsCount / (now - _avgCalcStart).TotalSeconds;

                    // Receives
                    var recv = Interlocked.Read(ref _received);
                    var recvDiff = recv - _lastReceivedCount;
                    var recvPerSec = recvDiff / timeDiffSecs;
                    _receivesPerSecond = recvPerSec;

                    _lastReceivedCount = recv;

                    Console.WriteLine("----- RECEIVES -----");

                    var d1 = Math.Max(0, TotalRate - _receivesPerSecond);
                    Console.WriteLine("RPS: {0:0.000} (diff: {1:0.000}, {2:0.00}%)", Math.Min(TotalRate, _receivesPerSecond), d1, d1 * 100.0 / TotalRate);
                    var d2 = Math.Max(0, TotalRate - _peakReceivesPerSecond);
                    Console.WriteLine("Peak RPS: {0:0.000} (diff: {1:0.000} {2:0.00}%)", Math.Min(TotalRate, _peakReceivesPerSecond), d2, d2 * 100.0 / TotalRate);
                    var d3 = Math.Max(0, TotalRate - _avgReceivesPerSecond);
                    Console.WriteLine("Avg RPS: {0:0.000} (diff: {1:0.000} {2:0.00}%)", Math.Min(TotalRate, _avgReceivesPerSecond), d3, d3 * 100.0 / TotalRate);

                    if (recvPerSec < long.MaxValue && recvPerSec > _peakReceivesPerSecond)
                    {
                        Interlocked.Exchange(ref _peakReceivesPerSecond, recvPerSec);
                    }

                    _avgReceivesPerSecond = _avgLastReceivedCount / (now - _avgCalcStart).TotalSeconds;

                    File.AppendAllText(resultsPath, String.Format("{0}, {1}, {2}, {3}\n", TotalRate, _receivesPerSecond, _peakReceivesPerSecond, _avgReceivesPerSecond));

                    if (_runs > 0 && _runs % _stepInterval == 0)
                    {
                        _avgCalcStart = DateTime.UtcNow;
                        Interlocked.Exchange(ref _avgLastReceivedCount, 0);
                        Interlocked.Exchange(ref _avgLastSendsCount, 0);
                        long old = Interlocked.Read(ref _rate);
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
