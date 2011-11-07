using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Infrastructure;

namespace SignalR
{
    public class Signaler
    {
        // REVIEW: Should we make this configurable?
        private static readonly TimeSpan _timeOutInterval = TimeSpan.FromSeconds(10);
        private static readonly object _timeOutCreationLock = new object();
        private static readonly SafeSet<TimeoutInfo> _timeOutInfos = new SafeSet<TimeoutInfo>();
        private static bool _timeOutCheckRunning;

        // Timer that runs on an interval to check for Subscription timeouts
        private static Timer _timeOutTimer;

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

            // Make sure the timer that checks for Subscription timeouts is running
            EnsureTimeoutTimer();

            var signalAction = new SafeHandleEventAndSetResultAction(SignalBus, tcs, eventKeys);

            foreach (var eventKey in eventKeys)
            {
                SignalBus.AddHandler(eventKey, signalAction.Handler);
            }

            if (cancellationToken != CancellationToken.None)
            {
                cancellationToken.Register(signalAction.SetCanceled);
            }

            // Create a new timeout info for this event
            var timeOutInfo = new TimeoutInfo(signalAction, DateTime.UtcNow, timeout);

            _timeOutInfos.Add(timeOutInfo);

            return tcs.Task;
        }

        private static void EnsureTimeoutTimer()
        {
            if (_timeOutTimer == null)
            {
                lock (_timeOutCreationLock)
                {
                    if (_timeOutTimer == null)
                    {
                        _timeOutTimer = new Timer(_ => CheckTimeouts(), null, TimeSpan.Zero, _timeOutInterval);
                    }
                }
            }
        }

        private static void CheckTimeouts()
        {
            if (_timeOutCheckRunning)
            {
                return;
            }

            _timeOutCheckRunning = true;

            foreach (TimeoutInfo timeoutInfo in _timeOutInfos.GetSnapshot())
            {
                if (timeoutInfo.TimedOut)
                {
                    // If we timed out the call the SetTimedOut method to complete the task
                    timeoutInfo.SignalAction.SetTimedOut();

                    // Remove this timeout info from the list
                    _timeOutInfos.Remove(timeoutInfo);
                }
            }

            _timeOutCheckRunning = false;
        }

        private class TimeoutInfo
        {
            private readonly DateTime _subscriptionTime;
            private readonly TimeSpan _timeout;

            public TimeoutInfo(SafeHandleEventAndSetResultAction signalAction,
                               DateTime subscriptionTime,
                               TimeSpan timeout)
            {
                SignalAction = signalAction;
                _subscriptionTime = subscriptionTime;
                _timeout = timeout;
            }

            public SafeHandleEventAndSetResultAction SignalAction { get; private set; }

            private TimeSpan Elapsed
            {
                get
                {
                    return DateTime.UtcNow - _subscriptionTime;
                }
            }

            public bool TimedOut
            {
                get
                {
                    return Elapsed > _timeout;
                }
            }
        }

        private class SafeHandleEventAndSetResultAction
        {
            private readonly object _locker;
            private readonly ISignalBus _signalBus;
            private readonly IEnumerable<string> _eventKeys;
            private bool _canceled;
            private bool _timedOut;
            private bool _handlerCalled;

            public SafeHandleEventAndSetResultAction(ISignalBus signalBus, TaskCompletionSource<SignalResult> tcs, IEnumerable<string> eventKeys)
            {
                _locker = new object();
                Handler = (sender, args) =>
                {
                    SafeHandleEventAndSetResult(args.EventKey);
                };
                Tcs = tcs;
                _signalBus = signalBus;
                _eventKeys = eventKeys;
            }

            public EventHandler<SignaledEventArgs> Handler { get; private set; }

            private TaskCompletionSource<SignalResult> Tcs { get; set; }

            private void SafeHandleEventAndSetResult(string signaledEventKey)
            {
                lock (_locker)
                {
                    if (_handlerCalled)
                    {
                        return;
                    }

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
                }
            }

            public void SetCanceled()
            {
                _canceled = true;
                SafeHandleEventAndSetResult(null);
            }

            public void SetTimedOut()
            {
                _timedOut = true;
                SafeHandleEventAndSetResult(null);
            }
        }
    }
}