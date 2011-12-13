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
        private static TimeSpan _timeOutInterval;
        private static TimeSpan _defaultTimeOut;

        private readonly object _timeOutCreationLock = new object();
        private readonly SafeSet<SafeHandleEventAndSetResultAction> _signalActions = new SafeSet<SafeHandleEventAndSetResultAction>();
        private bool _timeOutCheckRunning;

        // Timer that runs on an interval to check for Subscription timeouts
        private Timer _timeOutTimer;

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

        public TimeSpan DefaultTimeout
        {
            get
            {
                return _defaultTimeOut;
            }
            set
            {
                _defaultTimeOut = value;

                // Make the interval run for 1/2 the default timeout
                _timeOutInterval = TimeSpan.FromSeconds(value.TotalSeconds / 2);

                if (_timeOutTimer != null)
                {
                    // TODO: Adjust interval based on all timeouts
                    _timeOutTimer.Change(TimeSpan.Zero, _timeOutInterval);
                }
            }
        }

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

            var signalAction = new SafeHandleEventAndSetResultAction(SignalBus, tcs, eventKeys, timeout, action => _signalActions.Remove(action));

            foreach (var eventKey in eventKeys)
            {
                SignalBus.AddHandler(eventKey, signalAction.Handler);
            }

            if (cancellationToken != CancellationToken.None)
            {
                cancellationToken.Register(signalAction.SetCanceled);
            }

            _signalActions.Add(signalAction);

            // Make sure the timer that checks for Subscription timeouts is running
            EnsureTimeoutTimer();

            return tcs.Task;
        }

        private void EnsureTimeoutTimer()
        {
            if (_timeOutTimer == null)
            {
                lock (_timeOutCreationLock)
                {
                    if (_timeOutTimer == null)
                    {
                        _timeOutTimer = new Timer(_ => CheckTimeouts(), null, _timeOutInterval, _timeOutInterval);
                    }
                }
            }
        }

        private void CheckTimeouts()
        {
            if (_timeOutCheckRunning)
            {
                return;
            }

            _timeOutCheckRunning = true;

            foreach (var signalAction in _signalActions.GetSnapshot())
            {
                if (signalAction.TimeoutInfo.TimedOut)
                {
                    // If we timed out the call the SetTimedOut method to complete the task
                    signalAction.SetTimedOut();
                }
            }

            _timeOutCheckRunning = false;
        }

        private class TimeoutInfo
        {
            private readonly DateTime _subscriptionTime;
            private readonly TimeSpan _timeOut;

            public TimeoutInfo(SafeHandleEventAndSetResultAction signalAction,
                               DateTime subscriptionTime,
                               TimeSpan timeout)
            {
                SignalAction = signalAction;
                _timeOut = timeout;
                _subscriptionTime = subscriptionTime;
            }

            public SafeHandleEventAndSetResultAction SignalAction { get; private set; }

            public TimeSpan Elapsed
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
                    return Elapsed >= _timeOut;
                }
            }
        }

        private class SafeHandleEventAndSetResultAction
        {
            private static SignalResult _timedOutResult = new SignalResult { TimedOut = true };
            private readonly object _locker;
            private readonly ISignalBus _signalBus;
            private readonly IEnumerable<string> _eventKeys;
            private bool _canceled;
            private bool _timedOut;
            private bool _handlerCalled;
            private Action<SafeHandleEventAndSetResultAction> _removeCallback;

            public SafeHandleEventAndSetResultAction(ISignalBus signalBus,
                                                     TaskCompletionSource<SignalResult> tcs,
                                                     IEnumerable<string> eventKeys,
                                                     TimeSpan timeout,
                                                     Action<SafeHandleEventAndSetResultAction> removeCallback)
            {
                _locker = new object();
                TimeoutInfo = new TimeoutInfo(this, DateTime.UtcNow, timeout);
                Handler = SafeHandleEventAndSetResult;
                Tcs = tcs;
                _signalBus = signalBus;
                _eventKeys = eventKeys;
                _removeCallback = removeCallback;
            }

            public EventHandler<SignaledEventArgs> Handler { get; private set; }

            public TimeoutInfo TimeoutInfo { get; private set; }

            private TaskCompletionSource<SignalResult> Tcs { get; set; }

            private void SafeHandleEventAndSetResult(object source, SignaledEventArgs args)
            {
                SafeHandleEventAndSetResult(args.EventKey);
            }

            private void SafeHandleEventAndSetResult(string signaledEventKey)
            {
                // Only want to handle the signaled event once, then remove the handler from the bus
                if (_handlerCalled)
                {
                    return;
                }

                lock (_locker)
                {
                    if (_handlerCalled)
                    {
                        return;
                    }

                    _handlerCalled = true;
                }

                // TODO: Change this to SignalBus.RemoveHandler(IEnumerable<string> eventKeys, Func handler)
                //       Let the individual SignalBus implementations deal with how to do that
                foreach (var eventKey in _eventKeys)
                {
                    _signalBus.RemoveHandler(eventKey, Handler);
                }

                if (_canceled)
                {
                    Tcs.SetCanceled();
                }
                else if (_timedOut)
                {
                    Tcs.SetResult(_timedOutResult);
                }
                else
                {
                    Tcs.SetResult(new SignalResult
                    {
                        EventKey = signaledEventKey
                    });
                }

                _removeCallback(this);
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