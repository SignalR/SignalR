// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Threading;

    [DebuggerStepThrough]
    struct TimeoutHelper
    {
        DateTime deadline;
        bool deadlineSet;
        TimeSpan originalTimeout;
        public static readonly TimeSpan MaxWait = TimeSpan.FromMilliseconds(Int32.MaxValue);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This will be called in the future.")]
        public TimeoutHelper(TimeSpan timeout) :
            this(timeout, false)
        {
        }

        public TimeoutHelper(TimeSpan timeout, bool startTimeout)
        {
            Debug.Assert(timeout >= TimeSpan.Zero, "timeout must be non-negative");

            this.originalTimeout = timeout;
            this.deadline = DateTime.MaxValue;
            this.deadlineSet = (timeout == TimeSpan.MaxValue);

            if (startTimeout && !this.deadlineSet)
            {
                this.SetDeadline();
            }
        }

        public TimeSpan OriginalTimeout
        {
            get { return this.originalTimeout; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This will be called in the future.")]
        public static bool IsTooLarge(TimeSpan timeout)
        {
            return (timeout > TimeoutHelper.MaxWait) && (timeout != TimeSpan.MaxValue);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This will be called in the future.")]
        public static TimeSpan FromMilliseconds(int milliseconds)
        {
            if (milliseconds == Timeout.Infinite)
            {
                return TimeSpan.MaxValue;
            }
            else
            {
                return TimeSpan.FromMilliseconds(milliseconds);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This will be called in the future.")]
        public static int ToMilliseconds(TimeSpan timeout)
        {
            if (timeout == TimeSpan.MaxValue)
            {
                return Timeout.Infinite;
            }
            else
            {
                long ticks = Ticks.FromTimeSpan(timeout);
                if (ticks / TimeSpan.TicksPerMillisecond > int.MaxValue)
                {
                    return int.MaxValue;
                }
                return Ticks.ToMilliseconds(ticks);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This will be called in the future.")]
        public static TimeSpan Min(TimeSpan val1, TimeSpan val2)
        {
            if (val1 > val2)
            {
                return val2;
            }
            else
            {
                return val1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This will be called in the future.")]
        public static TimeSpan Add(TimeSpan timeout1, TimeSpan timeout2)
        {
            return Ticks.ToTimeSpan(Ticks.Add(Ticks.FromTimeSpan(timeout1), Ticks.FromTimeSpan(timeout2)));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This will be called in the future.")]
        public static DateTime Add(DateTime time, TimeSpan timeout)
        {
            if (timeout >= TimeSpan.Zero && DateTime.MaxValue - time <= timeout)
            {
                return DateTime.MaxValue;
            }
            if (timeout <= TimeSpan.Zero && DateTime.MinValue - time >= timeout)
            {
                return DateTime.MinValue;
            }
            return time + timeout;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This will be called in the future.")]
        public static DateTime Subtract(DateTime time, TimeSpan timeout)
        {
            return Add(time, TimeSpan.Zero - timeout);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This will be called in the future.")]
        public static TimeSpan Divide(TimeSpan timeout, int factor)
        {
            if (timeout == TimeSpan.MaxValue)
            {
                return TimeSpan.MaxValue;
            }

            return Ticks.ToTimeSpan((Ticks.FromTimeSpan(timeout) / factor) + 1);
        }

        public TimeSpan RemainingTime()
        {
            if (!this.deadlineSet)
            {
                this.SetDeadline();
                return this.originalTimeout;
            }
            else if (this.deadline == DateTime.MaxValue)
            {
                return TimeSpan.MaxValue;
            }
            else
            {
                TimeSpan remaining = this.deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    return TimeSpan.Zero;
                }
                else
                {
                    return remaining;
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This will be called in the future.")]
        public TimeSpan ElapsedTime()
        {
            return this.originalTimeout - this.RemainingTime();
        }

        void SetDeadline()
        {
            Debug.Assert(!deadlineSet, "TimeoutHelper deadline set twice.");
            this.deadline = DateTime.UtcNow + this.originalTimeout;
            this.deadlineSet = true;
        }

        public static void ThrowIfNegativeArgument(TimeSpan timeout)
        {
            ThrowIfNegativeArgument(timeout, "timeout");
        }

        public static void ThrowIfNegativeArgument(TimeSpan timeout, string argumentName)
        {
            if (timeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    argumentName,
                    timeout,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Error_ArgumentMustBeNonNegativeTimeoutVal,
                        argumentName,
                        timeout));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This will be called in the future.")]
        public static void ThrowIfNonPositiveArgument(TimeSpan timeout)
        {
            ThrowIfNonPositiveArgument(timeout, "timeout");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This will be called in the future.")]
        public static void ThrowIfNonPositiveArgument(TimeSpan timeout, string argumentName)
        {
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    argumentName,
                    timeout,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Error_ArgumentMustBePositiveTimeoutVal,
                        argumentName,
                        timeout));
            }
        }

        public static bool WaitOne(WaitHandle waitHandle, TimeSpan timeout)
        {
            ThrowIfNegativeArgument(timeout);
            if (timeout == TimeSpan.MaxValue)
            {
                waitHandle.WaitOne();
                return true;
            }
            else
            {
                return waitHandle.WaitOne(timeout, false);
            }
        }
    }
}
