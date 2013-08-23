namespace Microsoft.AspNet.SignalR.Tests.Common.Infrastructure
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    public static class CommonLog
    {
        public static void WriteLine(string format, params object[] args)
        {
            string message = string.Format(format, args);
            message = string.Format("{0}|{1}|{2}    {3}",
                DateTime.Now.ToString("HH:mm:ss.fff"),
                Thread.CurrentThread.ManagedThreadId.ToString("0000"),
                AppDomain.CurrentDomain.FriendlyName,
                message);

            Debug.WriteLine(message);
        }

        public static void WriteException(Exception exception)
        {
            CommonLog.WriteLine(exception.ToString());
        }

        public static void WriteExceptionMessage(Exception exception)
        {
            string message = string.Format("{0}: {1}", exception.GetType(), exception.Message);
            CommonLog.WriteLine(message);
        }

        public static void WriteExceptionMessage(Exception exception, string format, params object[] args)
        {
            string message = string.Format(format, args);
            string exceptionMessage = string.Format("{0}: {1}", exception.GetType(), exception.Message);
            message = string.Format("{0}. {1}", message, exceptionMessage);
            CommonLog.WriteLine(message);
        }
    }
}
