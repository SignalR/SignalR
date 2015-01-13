// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Hubs
{
    internal class HubInvocationProgress
    {
        private static readonly ConcurrentDictionary<Type, Func<Func<object, Task>, HubInvocationProgress>> _progressCreateCache = new ConcurrentDictionary<Type, Func<Func<object, Task>, HubInvocationProgress>>();

        private volatile bool _complete = false;

        private readonly object _statusLocker = new object();

        private readonly Func<object, Task> _sendProgressFunc;

        protected HubInvocationProgress(Func<object, Task> sendProgressFunc)
        {
            _sendProgressFunc = sendProgressFunc;
        }

        private TraceSource Trace { get; set; }

        public static HubInvocationProgress Create(Type progressGenericType, Func<object, Task> sendProgressFunc, TraceSource traceSource)
        {
            Func<Func<object, Task>, HubInvocationProgress> createDelegate;
            if (!_progressCreateCache.TryGetValue(progressGenericType, out createDelegate))
            {
                var createMethodInfo = typeof(HubInvocationProgress)
                    .GetMethod("Create", new[] { typeof(Func<object, Task>) })
                    .MakeGenericMethod(progressGenericType);

                createDelegate = (Func<Func<object, Task>, HubInvocationProgress>)createMethodInfo.CreateDelegate(typeof(Func<Func<object, Task>, HubInvocationProgress>));

                _progressCreateCache[progressGenericType] = createDelegate;
            }
            var progress = createDelegate.Invoke(sendProgressFunc);
            progress.Trace = traceSource;
            return progress;
        }

        public static HubInvocationProgress<T> Create<T>(Func<object, Task> sendProgressFunc)
        {
            return new HubInvocationProgress<T>(sendProgressFunc);
        }

        public void SetComplete()
        {
            lock (_statusLocker)
            {
                _complete = true;
            }
        }

        protected void DoReport(object value)
        {
            lock (_statusLocker)
            {
                if (_complete)
                {
                    throw new InvalidOperationException(Resources.Error_HubProgressOnlyReportableBeforeMethodReturns);
                }

                // Send progress update to client
                _sendProgressFunc(value).Catch(Trace);
            }
        }
    }

    internal class HubInvocationProgress<T> : HubInvocationProgress, IProgress<T>
    {
        public HubInvocationProgress(Func<object, Task> sendProgressFunc)
            : base(sendProgressFunc)
        {

        }

        public void Report(T value)
        {
            DoReport(value);
        }
    }
}
