﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    /// <summary>
    /// Allows for thread safe invocation of a delegate.
    /// </summary>
    internal class ThreadSafeInvoker
    {
        private int _invoked;

        public bool Invoke(Action action)
        {
            if (Interlocked.Exchange(ref _invoked, 1) == 0)
            {
                action();
                return true;
            }

            return false;
        }

        public bool Invoke<T>(Action<T> action, T arg)
        {
            if (Interlocked.Exchange(ref _invoked, 1) == 0)
            {
                action(arg);
                return true;
            }

            return false;
        }

        public bool Invoke()
        {
            return Invoke(() => { });
        }
    }
}
