using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SignalR.SignalBuses;
using SignalR.Infrastructure;

namespace SignalR {
    public class Signaler {
        private static readonly ConcurrentDictionary<string, Timer> _timers = new ConcurrentDictionary<string, Timer>();
        private static readonly Signaler _instance = new Signaler();
        
        public Signaler() {
            DefaultTimeout = TimeSpan.FromMinutes(2);
        }

        public static Signaler Instance {
            get {
                return _instance;
            }
        }

        public virtual ISignalBus SignalBus {
            get {
                return DependencyResolver.Resolve<ISignalBus>();
            }
        }

        public virtual TimeSpan DefaultTimeout { get; set; }

        public virtual Task Signal(string eventKey) {
            return SignalBus.Signal(eventKey);
        }

        public virtual Task<SignalResult> Subscribe(string eventKey) {
            return Subscribe(DefaultTimeout, eventKey);
        }

        public virtual Task<SignalResult> Subscribe(IEnumerable<string> eventKeys) {
            return Subscribe(eventKeys.ToArray());
        }

        public virtual Task<SignalResult> Subscribe(TimeSpan timeout, string eventKey) {
            return Subscribe(timeout, new[] { eventKey });
        }

        public virtual Task<SignalResult> Subscribe(params string[] eventKeys) {
            return Subscribe(DefaultTimeout, eventKeys);
        }

        public virtual Task<SignalResult> Subscribe(TimeSpan timeout, IEnumerable<string> eventKeys) {
            return Subscribe(timeout, CancellationToken.None, eventKeys);
        }

        public virtual Task<SignalResult> Subscribe(TimeSpan timeout, CancellationToken cancellationToken, IEnumerable<string> eventKeys) {
            return Subscribe(timeout, cancellationToken, eventKeys.ToArray());
        }

        public virtual Task<SignalResult> Subscribe(TimeSpan timeout, CancellationToken cancellationToken, params string[] eventKeys) {
            var tcs = new TaskCompletionSource<SignalResult>();

            var handlerCalled = false;
            var handlerLock = new object();

            EventHandler<SignaledEventArgs> onSignaled = null;
            var timerKey = Guid.NewGuid().ToString();

            onSignaled = (source, e) => {
                SafeHandleEventAndSetResult(handlerLock, onSignaled, ref handlerCalled, tcs, timerKey, e.EventKey, canceled: false, timedOut: false);
            };

            foreach (var eventKey in eventKeys) {
                SignalBus.AddHandler(eventKey, onSignaled);
            }

            if (cancellationToken != CancellationToken.None) {
                cancellationToken.Register(() => SafeHandleEventAndSetResult(handlerLock, onSignaled, ref handlerCalled, tcs, timerKey, eventKey: null, canceled: true, timedOut: false));
            }

            var timer = new Timer(
                        _ => SafeHandleEventAndSetResult(handlerLock, onSignaled, ref handlerCalled, tcs, timerKey, eventKey: null, canceled: false, timedOut: true),
                        null,
                        timeout,
                        timeout);

            _timers.TryAdd(timerKey, timer);

            return tcs.Task;
        }

        private void SafeHandleEventAndSetResult(
            object locker,
            EventHandler<SignaledEventArgs> handler,
            ref bool handlerCalled,
            TaskCompletionSource<SignalResult> tcs,
            string timerKey,
            string eventKey,
            bool canceled,
            bool timedOut) {
            lock (locker) {
                if (!handlerCalled) {
                    handlerCalled = true;

                    if (!String.IsNullOrEmpty(eventKey)) {
                        SignalBus.RemoveHandler(eventKey, handler);
                    }

                    if (canceled) {
                        tcs.SetCanceled();
                    }
                    else {
                        tcs.SetResult(new SignalResult { TimedOut = timedOut, EventKey = eventKey });
                    }

                    Timer timer;
                    if (_timers.TryRemove(timerKey, out timer)) {
                        timer.Dispose();
                    }
                }
            }
        }
    }
}