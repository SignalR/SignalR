using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR
{
    public static class TestTraceManager
    {
        public static readonly string LogBasePath = Environment.GetEnvironmentVariable("SIGNALR_TEST_LOG_BASE");
        public static readonly bool IsEnabled = !string.IsNullOrEmpty(LogBasePath);

        public static string GetTraceFilePath(string logName)
        {
            if (IsEnabled)
            {
                return Path.Combine(LogBasePath, logName);
            }
            else
            {
                return null;
            }
        }

        public static IDisposable CreateTraceListener(string logName)
        {
            if (IsEnabled)
            {
                var traceListener = new TextWriterTraceListener(GetTraceFilePath(logName));
                Trace.Listeners.Add(traceListener);
                Trace.AutoFlush = true;
                return new DisposableAction(() =>
                {
                    traceListener.Close();
                    Trace.Listeners.Remove(traceListener);
                });
            }
            return DisposableAction.Empty;
        }
    }
}
