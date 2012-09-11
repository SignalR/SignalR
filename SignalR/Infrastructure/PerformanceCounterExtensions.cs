using System.Diagnostics;

namespace SignalR.Infrastructure
{
    internal static class PerformanceCounterExtensions
    {
        public static long? SafeIncrement(this PerformanceCounter counter)
        {
            if (counter != null)
            {
                return counter.Increment();
            }
            return null;
        }

        public static long? SafeDecrement(this PerformanceCounter counter)
        {
            if (counter != null)
            {
                return counter.Decrement();
            }
            return null;
        }

        public static long? SafeIncrementBy(this PerformanceCounter counter, long value)
        {
            if (counter != null)
            {
                return counter.IncrementBy(value);
            }
            return null;
        }

        public static void SafeSetRaw(this PerformanceCounter counter, long value)
        {
            if (counter != null)
            {
                counter.RawValue = value;
            }
        }
    }
}
