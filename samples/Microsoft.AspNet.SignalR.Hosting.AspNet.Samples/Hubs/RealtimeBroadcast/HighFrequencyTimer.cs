using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.AspNet.SignalR
{
    /// <summary>
    /// A high-resolution FPS timer.
    /// </summary>
    public class HighFrequencyTimer
    {
        private long _fps;
        private long _frameId;
        private Action<long> _tick;
        private Action _started;
        private Action _stopped;
        private Action<int> _actualFpsUpdate;

        // 0 = stopped
        // 1 = start requested
        // 2 = running
        // 3 = stop requested
        private long _state = 0;

        /// <summary>
        /// Creates a new instance of a high-resolution FPS timer.
        /// </summary>
        /// <param name="fps">The desired frame rate per second.</param>
        /// <param name="tick">The callback to be invoked on each frame.</param>
        public HighFrequencyTimer(int fps, Action<long> tick)
            : this(fps, tick, null, null, null)
        {
            
        }

        /// <summary>
        /// Creates a new instance of a high-resolution FPS timer.
        /// </summary>
        /// <param name="fps">The desired frame rate per second.</param>
        /// <param name="tick">The callback to be invoked on each frame.</param>
        /// <param name="started">The callback to be invoked when the timer enters the running state.</param>
        /// <param name="stopped">The callback to be invoked when the timer enters the stopped state.</param>
        /// <param name="actualFpsUpdate">The callback to invoked to receive updates of the actual frame rate.</param>
        public HighFrequencyTimer(int fps, Action<long> tick, Action started, Action stopped, Action<int> actualFpsUpdate)
        {
            if (fps <= 0)
            {
                throw new ArgumentException("Value must be greater than zero.", "fps");
            }

            if (tick == null)
            {
                throw new ArgumentNullException("tick");
            }

            _fps = fps;
            _tick = tick;
            _started = started ?? (() => { });
            _stopped = stopped ?? (() => { });
            _actualFpsUpdate = actualFpsUpdate ?? (_ => { });
        }

        public bool Start()
        {
            // Try to move to starting state
            if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
            {
                // Wasn't stopped
                return false;
            }

            // In the starting state now so start the loop
            ThreadPool.QueueUserWorkItem(Run);

            return true;
        }

        public bool Stop()
        {
            // Try to move to stopping state
            if (Interlocked.CompareExchange(ref _state, 3, 2) != 2)
            {
                // Wasn't running
                return false;
            }

            return true;
        }

        public long FPS
        {
            get { return Interlocked.Read(ref _fps); }
            set { Interlocked.Exchange(ref _fps, value); }
        }

        public long GetFrameId()
        {
            return Interlocked.Read(ref _frameId);
        }

        public bool IsRunning()
        {
            return Interlocked.Read(ref _state) == 2;
        }

        private void Run(object state)
        {
            long lastMs = 0;
            var sw = Stopwatch.StartNew();
            long lastFpsCheck = 0;
            var actualFps = 0;

            // Move to running state
            Interlocked.Exchange(ref _state, 2);
            _started();

            while (Interlocked.Read(ref _state) == 2)
            {
                var frameMs = (int)Math.Round(1000.0 / Interlocked.Read(ref _fps));
                long delta = (lastMs + frameMs) - sw.ElapsedMilliseconds;
                if (delta <= 0)
                {
                    Interlocked.Increment(ref actualFps);
                    _tick(Interlocked.Increment(ref _frameId));
                    lastMs = sw.ElapsedMilliseconds;

                    // Actual FPS check, update every second
                    if ((lastFpsCheck + 1000 - sw.ElapsedMilliseconds) <= 0)
                    {
                        _actualFpsUpdate(actualFps);
                        lastFpsCheck = sw.ElapsedMilliseconds;
                        Interlocked.Exchange(ref actualFps, 0);
                    }
                }
                else
                {
                    Thread.Yield();
                }
            }

            // Move to stopped state
            Interlocked.Exchange(ref _state, 0);
            Interlocked.Exchange(ref _frameId, 0);
            sw.Stop();
            _stopped();
        }
    }
}