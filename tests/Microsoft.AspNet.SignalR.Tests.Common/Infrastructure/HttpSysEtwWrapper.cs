using System;
using System.Diagnostics;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure
{
    public class HttpSysEtwWrapper : IDisposable
    {
        private readonly string _filePath;

        public HttpSysEtwWrapper(string filePath)
        {
            _filePath = filePath;
        }

        public bool StartLogging()
        {
            var psi = new ProcessStartInfo
            {
                FileName = "logman",
                Arguments = "start httptrace -p Microsoft-Windows-HttpService 0xFFFF -o " + _filePath + ".etl -ets",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(psi);
            process.WaitForExit();
            return process.ExitCode == 0;
        }

        public void Dispose()
        {
            var psi = new ProcessStartInfo
            {
                FileName = "logman",
                Arguments = "stop httptrace -ets",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(psi);
            process.WaitForExit();
        }
    }
}
