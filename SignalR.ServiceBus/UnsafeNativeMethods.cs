namespace SignalR.ServiceBus
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;

    static class UnsafeNativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void GetSystemTimeAsFileTime([Out] out long time);

        [DllImport("kernel32.dll", SetLastError = false)]
        public static extern uint GetSystemTimeAdjustment(
            [Out] out int adjustment,
            [Out] out uint increment,
            [Out] out uint adjustmentDisabled);

        [DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Auto)]
        public static extern SafeWaitHandle CreateWaitableTimer(IntPtr mustBeZero, bool manualReset, string timerName);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern bool SetWaitableTimer(SafeWaitHandle handle, ref long dueTime, int period, IntPtr mustBeZero, IntPtr mustBeZeroAlso, bool resume);
    }
}
