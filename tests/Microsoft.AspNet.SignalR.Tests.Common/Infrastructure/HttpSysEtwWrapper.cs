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

        public void StartLogging()
        {
            Process.Start("logman", "start httptrace -p Microsoft-Windows-HttpService 0xFFFF -o " + _filePath + ".etl -ets");
        }

        public void Dispose()
        {
            Process.Start("logman", "stop httptrace -ets");
        }
    }
}
