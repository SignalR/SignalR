// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System;

    static class Ticks
    {
        public static long Now
        {
            get
            {
                long time;
                UnsafeNativeMethods.GetSystemTimeAsFileTime(out time);
                return time;
            }
        }

        public static long FromMilliseconds(int milliseconds)
        {
            return checked(milliseconds * TimeSpan.TicksPerMillisecond);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This will be called in the future.")]
        public static int ToMilliseconds(long ticks)
        {
            return checked((int)(ticks / TimeSpan.TicksPerMillisecond));
        }

        public static long FromTimeSpan(TimeSpan duration)
        {
            return duration.Ticks;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This will be called in the future.")]
        public static TimeSpan ToTimeSpan(long ticks)
        {
            return new TimeSpan(ticks);
        }

        public static long Add(long firstTicks, long secondTicks)
        {
            if (firstTicks == long.MaxValue || firstTicks == long.MinValue)
            {
                return firstTicks;
            }

            if (secondTicks == long.MaxValue || secondTicks == long.MinValue)
            {
                return secondTicks;
            }

            if (firstTicks >= 0 && long.MaxValue - firstTicks <= secondTicks)
            {
                return long.MaxValue - 1;
            }

            if (firstTicks <= 0 && long.MinValue - firstTicks >= secondTicks)
            {
                return long.MinValue + 1;
            }

            return checked(firstTicks + secondTicks);
        }
    }
}
