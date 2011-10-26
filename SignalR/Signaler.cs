using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    public class Signaler
    {
        private static readonly ConcurrentDictionary<string, Timer> _timers = new ConcurrentDictionary<string, Timer>();
        private static readonly Signaler _instance = new Signaler();

        public Signaler()
        {
            DefaultTimeout = TimeSpan.FromMinutes(2);
        }

        public static Signaler Instance
        {
            get
            {
                return _instance;
            }
        }

        public virtual ISignalBus SignalBus
        {
            get
            {
                return DependencyResolver.Resolve<ISignalBus>();
            }
        }

        public TimeSpan DefaultTimeout { get; set; }

        public virtual Task Signal(string eventKey)
        {
            return SignalBus.Signal(eventKey);
        }

        public virtual Task<SignalResult> Subscribe(string eventKey)
        {
            return Subscribe(DefaultTimeout, eventKey);
        }

        public virtual Task<SignalResult> Subscribe(IEnumerable<string> eventKeys)
        {
            return Subscribe(eventKeys.ToArray());
        }

        public virtual Task<SignalResult> Subscribe(TimeSpan timeout, string eventKey)
        {
            return Subscribe(timeout, new[] { eventKey });
        }

        public virtual Task<SignalResult> Subscribe(params string[] eventKeys)
        {
            return Subscribe(DefaultTimeout, eventKeys);
        }

        public virtual Task<SignalResult> Subscribe(TimeSpan timeout, IEnumerable<string> eventKeys)
        {
            return Subscribe(timeout, CancellationToken.None, eventKeys);
        }

        public virtual Task<SignalResult> Subscribe(TimeSpan timeout, CancellationToken cancellationToken, IEnumerable<string> eventKeys)
        {
            return Subscribe(timeout, cancellationToken, eventKeys.ToArray());
        }

        public virtual Task<SignalResult> Subscribe(TimeSpan timeout, CancellationToken cancellationToken, params string[] eventKeys)
        {
            var tcs = new TaskCompletionSource<SignalResult>();
            var timerKey = Guid.NewGuid().ToString();

            var signalAction = new SafeHandleEventAndSetResultAction(SignalBus, tcs, timerKey, eventKeys);

            foreach (var eventKey in eventKeys)
            {
                SignalBus.AddHandler(eventKey, signalAction.Handler);
            }

            if (cancellationToken != CancellationToken.None)
            {
                cancellationToken.Register(signalAction.SetCanceled);
            }

            var timer = new Timer(signalAction.SetTimedOut,
                        null,
                        timeout,
                        timeout);

            _timers.TryAdd(timerKey, timer);

            return tcs.Task;
        }

        private class SafeHandleEventAndSetResultAction
        {
            private readonly object _locker;
            private readonly ISignalBus _signalBus;
            private readonly string _timerKey;
            private readonly IEnumerable<string> _eventKeys;
            private bool _canceled;
            private bool _timedOut;
            private bool _handlerCalled;

            public SafeHandleEventAndSetResultAction(ISignalBus signalBus, TaskCompletionSource<SignalResult> tcs, string timerKey, IEnumerable<string> eventKeys)
            {
                _locker = new object();
                Handler = (sender, args) =>
                {
                    SafeHandleEventAndSetResult(args.EventKey);
                };
                Tcs = tcs;
                _signalBus = signalBus;
                _timerKey = timerKey;
                _eventKeys = eventKeys;
            }

            public EventHandler<SignaledEventArgs> Handler { get; private set; }

            private TaskCompletionSource<SignalResult> Tcs { get; set; }

            private void SafeHandleEventAndSetResult(string signaledEventKey)
            {
                lock (_locker)
                {
                    if (_handlerCalled)
                        return;

                    _handlerCalled = true;

                    foreach (var eventKey in _eventKeys)
                    {
                        _signalBus.RemoveHandler(eventKey, Handler);
                    }

                    if (_canceled)
                    {
                        Tcs.SetCanceled();
                    }
                    else
                    {
                        Tcs.SetResult(new SignalResult { TimedOut = _timedOut, EventKey = signaledEventKey });
                    }

                    Timer timer;
                    if (_timers.TryRemove(_timerKey, out timer))
                    {
                        timer.Dispose();
                    }
                }
            }

            public void SetCanceled()
            {
                _canceled = true;
                SafeHandleEventAndSetResult(null);
            }

            public void SetTimedOut(object state)
            {
                _timedOut = true;
                SafeHandleEventAndSetResult(null);
            }
        }

    }
}