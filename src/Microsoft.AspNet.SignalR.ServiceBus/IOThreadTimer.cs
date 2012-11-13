// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Threading;
    using Microsoft.Win32.SafeHandles;

    class IOThreadTimer
    {
        const int maxSkewInMillisecondsDefault = 100;
        static long systemTimeResolutionTicks = -1;
        Action<object> callback;
        object callbackState;
        long dueTime;

        int index;
        long maxSkew;
        TimerGroup timerGroup;

        public IOThreadTimer(Action<object> callback, object callbackState, bool isTypicallyCanceledShortlyAfterBeingSet)
            : this(callback, callbackState, isTypicallyCanceledShortlyAfterBeingSet, maxSkewInMillisecondsDefault)
        {
        }

        public IOThreadTimer(Action<object> callback, object callbackState, bool isTypicallyCanceledShortlyAfterBeingSet, int maxSkewInMilliseconds)
        {
            this.callback = callback;
            this.callbackState = callbackState;
            this.maxSkew = Ticks.FromMilliseconds(maxSkewInMilliseconds);
            this.timerGroup =
                (isTypicallyCanceledShortlyAfterBeingSet ? TimerManager.Value.VolatileTimerGroup : TimerManager.Value.StableTimerGroup);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Will be used in the future.")]
        public static long SystemTimeResolutionTicks
        {
            get
            {
                if (IOThreadTimer.systemTimeResolutionTicks == -1)
                {
                    IOThreadTimer.systemTimeResolutionTicks = GetSystemTimeResolution();
                }
                return IOThreadTimer.systemTimeResolutionTicks;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Will be used in the future.")]
        static long GetSystemTimeResolution()
        {
            int dummyAdjustment;
            uint increment;
            uint dummyAdjustmentDisabled;

            if (UnsafeNativeMethods.GetSystemTimeAdjustment(out dummyAdjustment, out increment, out dummyAdjustmentDisabled) != 0)
            {
                return increment;
            }

            // Assume the default, which is around 15 milliseconds.
            return 15 * TimeSpan.TicksPerMillisecond;
        }

        public bool Cancel()
        {
            return TimerManager.Value.Cancel(this);
        }

        public void Set(TimeSpan timeFromNow)
        {
            if (timeFromNow == TimeSpan.MaxValue)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_IOThreadTimerCannotAcceptTimeSpanMaxVal), "timeFromNow");
            }

            SetAt(Ticks.Add(Ticks.Now, Ticks.FromTimeSpan(timeFromNow)));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Will be used in the future.")]
        public void Set(int millisecondsFromNow)
        {
            SetAt(Ticks.Add(Ticks.Now, Ticks.FromMilliseconds(millisecondsFromNow)));
        }

        public void SetAt(long newDueTime)
        {
            if (newDueTime >= TimeSpan.MaxValue.Ticks || newDueTime < 0)
            {
                string errorMessage = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Error_ValueSuppliedMustBeBetween,
                    0,
                    TimeSpan.MaxValue.Ticks - 1);

                throw new ArgumentOutOfRangeException("newDueTime", newDueTime, errorMessage);
            }

            TimerManager.Value.Set(this, newDueTime);
        }

        class TimerManager : IDisposable
        {
            const long maxTimeToWaitForMoreTimers = 1000 * TimeSpan.TicksPerMillisecond;
            static TimerManager value = new TimerManager();

            Action<object> onWaitCallback;
            TimerGroup stableTimerGroup;
            TimerGroup volatileTimerGroup;
            WaitableTimer[] waitableTimers;

            bool waitScheduled;

            public TimerManager()
            {
                this.onWaitCallback = new Action<object>(OnWaitCallback);
                this.stableTimerGroup = new TimerGroup();
                this.volatileTimerGroup = new TimerGroup();
                this.waitableTimers = new WaitableTimer[] { this.stableTimerGroup.WaitableTimer, this.volatileTimerGroup.WaitableTimer };
            }

            object ThisLock
            {
                get { return this; }
            }

            public static TimerManager Value
            {
                get
                {
                    return TimerManager.value;
                }
            }

            public TimerGroup StableTimerGroup
            {
                get
                {
                    return this.stableTimerGroup;
                }
            }
            public TimerGroup VolatileTimerGroup
            {
                get
                {
                    return this.volatileTimerGroup;
                }
            }

            public void Set(IOThreadTimer timer, long dueTime)
            {
                long timeDiff = dueTime - timer.dueTime;
                if (timeDiff < 0)
                {
                    timeDiff = -timeDiff;
                }

                if (timeDiff > timer.maxSkew)
                {
                    lock (ThisLock)
                    {
                        TimerGroup timerGroup = timer.timerGroup;
                        TimerQueue timerQueue = timerGroup.TimerQueue;

                        if (timer.index > 0)
                        {
                            if (timerQueue.UpdateTimer(timer, dueTime))
                            {
                                UpdateWaitableTimer(timerGroup);
                            }
                        }
                        else
                        {
                            if (timerQueue.InsertTimer(timer, dueTime))
                            {
                                UpdateWaitableTimer(timerGroup);

                                if (timerQueue.Count == 1)
                                {
                                    EnsureWaitScheduled();
                                }
                            }
                        }
                    }
                }
            }

            public bool Cancel(IOThreadTimer timer)
            {
                lock (ThisLock)
                {
                    if (timer.index > 0)
                    {
                        TimerGroup timerGroup = timer.timerGroup;
                        TimerQueue timerQueue = timerGroup.TimerQueue;

                        timerQueue.DeleteTimer(timer);

                        if (timerQueue.Count > 0)
                        {
                            UpdateWaitableTimer(timerGroup);
                        }
                        else
                        {
                            TimerGroup otherTimerGroup = GetOtherTimerGroup(timerGroup);
                            if (otherTimerGroup.TimerQueue.Count == 0)
                            {
                                long now = Ticks.Now;
                                long thisGroupRemainingTime = timerGroup.WaitableTimer.DueTime - now;
                                long otherGroupRemainingTime = otherTimerGroup.WaitableTimer.DueTime - now;
                                if (thisGroupRemainingTime > maxTimeToWaitForMoreTimers &&
                                    otherGroupRemainingTime > maxTimeToWaitForMoreTimers)
                                {
                                    timerGroup.WaitableTimer.Set(Ticks.Add(now, maxTimeToWaitForMoreTimers));
                                }
                            }
                        }

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            void EnsureWaitScheduled()
            {
                if (!this.waitScheduled)
                {
                    ScheduleWait();
                }
            }

            TimerGroup GetOtherTimerGroup(TimerGroup timerGroup)
            {
                if (object.ReferenceEquals(timerGroup, this.volatileTimerGroup))
                {
                    return this.stableTimerGroup;
                }
                else
                {
                    return this.volatileTimerGroup;
                }
            }

            void OnWaitCallback(object state)
            {
                WaitHandle.WaitAny(this.waitableTimers);
                long now = Ticks.Now;
                lock (ThisLock)
                {
                    this.waitScheduled = false;
                    ScheduleElapsedTimers(now);
                    ReactivateWaitableTimers();
                    ScheduleWaitIfAnyTimersLeft();
                }
            }

            void ReactivateWaitableTimers()
            {
                ReactivateWaitableTimer(this.stableTimerGroup);
                ReactivateWaitableTimer(this.volatileTimerGroup);
            }

            static void ReactivateWaitableTimer(TimerGroup timerGroup)
            {
                TimerQueue timerQueue = timerGroup.TimerQueue;

                if (timerQueue.Count > 0)
                {
                    timerGroup.WaitableTimer.Set(timerQueue.MinTimer.dueTime);
                }
                else
                {
                    timerGroup.WaitableTimer.Set(long.MaxValue);
                }
            }

            void ScheduleElapsedTimers(long now)
            {
                ScheduleElapsedTimers(this.stableTimerGroup, now);
                ScheduleElapsedTimers(this.volatileTimerGroup, now);
            }

            static void ScheduleElapsedTimers(TimerGroup timerGroup, long now)
            {
                TimerQueue timerQueue = timerGroup.TimerQueue;
                while (timerQueue.Count > 0)
                {
                    IOThreadTimer timer = timerQueue.MinTimer;
                    long timeDiff = timer.dueTime - now;
                    if (timeDiff <= timer.maxSkew)
                    {
                        timerQueue.DeleteMinTimer();
                        IOThreadScheduler.ScheduleCallbackNoFlow(timer.callback, timer.callbackState);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            void ScheduleWait()
            {
                IOThreadScheduler.ScheduleCallbackNoFlow(this.onWaitCallback, null);
                this.waitScheduled = true;
            }

            void ScheduleWaitIfAnyTimersLeft()
            {
                if (this.stableTimerGroup.TimerQueue.Count > 0 ||
                    this.volatileTimerGroup.TimerQueue.Count > 0)
                {
                    ScheduleWait();
                }
            }

            static void UpdateWaitableTimer(TimerGroup timerGroup)
            {
                WaitableTimer waitableTimer = timerGroup.WaitableTimer;
                IOThreadTimer minTimer = timerGroup.TimerQueue.MinTimer;
                long timeDiff = waitableTimer.DueTime - minTimer.dueTime;
                if (timeDiff < 0)
                {
                    timeDiff = -timeDiff;
                }
                if (timeDiff > minTimer.maxSkew)
                {
                    waitableTimer.Set(minTimer.dueTime);
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.stableTimerGroup.Dispose();
                    this.volatileTimerGroup.Dispose();
                    GC.SuppressFinalize(this);
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
        }

        class TimerGroup : IDisposable
        {
            TimerQueue timerQueue;
            WaitableTimer waitableTimer;

            public TimerGroup()
            {
                this.waitableTimer = new WaitableTimer();
                this.waitableTimer.Set(long.MaxValue);
                this.timerQueue = new TimerQueue();
            }

            public TimerQueue TimerQueue
            {
                get
                {
                    return this.timerQueue;
                }
            }
            public WaitableTimer WaitableTimer
            {
                get
                {
                    return this.waitableTimer;
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.waitableTimer.Dispose();
                    GC.SuppressFinalize(this);
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
        }

        class TimerQueue
        {
            int count;
            IOThreadTimer[] timers;

            public TimerQueue()
            {
                this.timers = new IOThreadTimer[4];
            }

            public int Count
            {
                get { return count; }
            }

            public IOThreadTimer MinTimer
            {
                get
                {
                    return timers[1];
                }
            }
            public void DeleteMinTimer()
            {
                IOThreadTimer minTimer = this.MinTimer;
                DeleteMinTimerCore();
                minTimer.index = 0;
                minTimer.dueTime = 0;
            }
            public void DeleteTimer(IOThreadTimer timer)
            {
                int index = timer.index;

                IOThreadTimer[] tempTimers = this.timers;

                for (; ; )
                {
                    int parentIndex = index / 2;

                    if (parentIndex >= 1)
                    {
                        IOThreadTimer parentTimer = tempTimers[parentIndex];
                        tempTimers[index] = parentTimer;
                        parentTimer.index = index;
                    }
                    else
                    {
                        break;
                    }

                    index = parentIndex;
                }

                timer.index = 0;
                timer.dueTime = 0;
                tempTimers[1] = null;
                DeleteMinTimerCore();
            }

            public bool InsertTimer(IOThreadTimer timer, long dueTime)
            {
                IOThreadTimer[] tempTimers = this.timers;

                int index = this.count + 1;

                if (index == tempTimers.Length)
                {
                    tempTimers = new IOThreadTimer[tempTimers.Length * 2];
                    Array.Copy(this.timers, tempTimers, this.timers.Length);
                    this.timers = tempTimers;
                }

                this.count = index;

                if (index > 1)
                {
                    for (; ; )
                    {
                        int parentIndex = index / 2;

                        if (parentIndex == 0)
                        {
                            break;
                        }

                        IOThreadTimer parent = tempTimers[parentIndex];

                        if (parent.dueTime > dueTime)
                        {
                            tempTimers[index] = parent;
                            parent.index = index;
                            index = parentIndex;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                tempTimers[index] = timer;
                timer.index = index;
                timer.dueTime = dueTime;
                return index == 1;
            }
            public bool UpdateTimer(IOThreadTimer timer, long newDueTime)
            {
                int index = timer.index;

                IOThreadTimer[] tempTimers = this.timers;
                int tempCount = this.count;

                int parentIndex = index / 2;
                if (parentIndex == 0 ||
                    tempTimers[parentIndex].dueTime <= newDueTime)
                {
                    int leftChildIndex = index * 2;
                    if (leftChildIndex > tempCount ||
                        tempTimers[leftChildIndex].dueTime >= newDueTime)
                    {
                        int rightChildIndex = leftChildIndex + 1;
                        if (rightChildIndex > tempCount ||
                            tempTimers[rightChildIndex].dueTime >= newDueTime)
                        {
                            timer.dueTime = newDueTime;
                            return index == 1;
                        }
                    }
                }

                DeleteTimer(timer);
                InsertTimer(timer, newDueTime);
                return true;
            }

            void DeleteMinTimerCore()
            {
                int currentCount = this.count;

                if (currentCount == 1)
                {
                    this.count = 0;
                    this.timers[1] = null;
                }
                else
                {
                    IOThreadTimer[] tempTimers = this.timers;
                    IOThreadTimer lastTimer = tempTimers[currentCount];
                    this.count = --currentCount;

                    int index = 1;
                    for (; ; )
                    {
                        int leftChildIndex = index * 2;

                        if (leftChildIndex > currentCount)
                        {
                            break;
                        }

                        int childIndex;
                        IOThreadTimer child;

                        if (leftChildIndex < currentCount)
                        {
                            IOThreadTimer leftChild = tempTimers[leftChildIndex];
                            int rightChildIndex = leftChildIndex + 1;
                            IOThreadTimer rightChild = tempTimers[rightChildIndex];

                            if (rightChild.dueTime < leftChild.dueTime)
                            {
                                child = rightChild;
                                childIndex = rightChildIndex;
                            }
                            else
                            {
                                child = leftChild;
                                childIndex = leftChildIndex;
                            }
                        }
                        else
                        {
                            childIndex = leftChildIndex;
                            child = tempTimers[childIndex];
                        }

                        if (lastTimer.dueTime > child.dueTime)
                        {
                            tempTimers[index] = child;
                            child.index = index;
                        }
                        else
                        {
                            break;
                        }

                        index = childIndex;

                        if (leftChildIndex >= currentCount)
                        {
                            break;
                        }
                    }

                    tempTimers[index] = lastTimer;
                    lastTimer.index = index;
                    tempTimers[currentCount + 1] = null;
                }
            }
        }

        class WaitableTimer : WaitHandle
        {

            long dueTime;

            public WaitableTimer()
            {
                this.SafeWaitHandle = TimerHelper.CreateWaitableTimer();
            }

            public long DueTime
            {
                get { return this.dueTime; }
            }

            public void Set(long newDueTime)
            {
                this.dueTime = TimerHelper.Set(this.SafeWaitHandle, newDueTime);
            }

            static class TimerHelper
            {
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Do not want to alter functionality.")]
                public static SafeWaitHandle CreateWaitableTimer()
                {
                    SafeWaitHandle handle = UnsafeNativeMethods.CreateWaitableTimer(IntPtr.Zero, false, null);
                    if (handle.IsInvalid)
                    {
                        Exception exception = new Win32Exception();
                        handle.SetHandleAsInvalid();
                        throw exception;
                    }
                    return handle;
                }
                public static long Set(SafeWaitHandle timer, long dueTime)
                {
                    if (!UnsafeNativeMethods.SetWaitableTimer(timer, ref dueTime, 0, IntPtr.Zero, IntPtr.Zero, false))
                    {
                        throw new Win32Exception();
                    }
                    return dueTime;
                }
            }
        }
    }
}
