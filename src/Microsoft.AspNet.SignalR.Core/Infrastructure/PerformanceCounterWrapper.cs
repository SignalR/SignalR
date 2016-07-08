// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    internal class PerformanceCounterWrapper : IPerformanceCounter
    {
        private readonly PerformanceCounter _counter;

        public PerformanceCounterWrapper(PerformanceCounter counter)
        {
            _counter = counter;
        }

        public string CounterName
        {
            get
            {
                return _counter.CounterName;
            }
        }

        public long RawValue
        {
            get { return _counter.RawValue; }
            set { _counter.RawValue = value; }
        }

        public long Decrement()
        {
            return _counter.Decrement();
        }

        public long Increment()
        {
            return _counter.Increment();
        }

        public long IncrementBy(long value)
        {
            return _counter.IncrementBy(value);
        }

        public void Close()
        {
            _counter.Close();
        }

        public void RemoveInstance()
        {
            try
            {
                _counter.RemoveInstance();
            }
            catch(NotImplementedException)
            {
            	// This happens on mono
            }
        }

        public CounterSample NextSample()
        {
            return _counter.NextSample();
        }
    }
}
