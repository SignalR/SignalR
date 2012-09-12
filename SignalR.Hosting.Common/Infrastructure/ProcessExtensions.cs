using System;
using System.Diagnostics;
using System.Threading;

namespace SignalR.Hosting.Common
{
    public static class ProcessExtensions
    {
        public static string GetUniqueInstanceName(this Process process, CancellationToken release)
        {
            Mutex mutex = null;
            
            var instanceId = 0;
            while (true)
            {
                // TODO: Need to distinguish between different instances of a process
                var mutexName = process.ProcessName + instanceId;
                bool createdMutex;
                // Try to create the mutex with ownership
                try
                {
                    mutex = new Mutex(true, mutexName, out createdMutex);
                    if (createdMutex)
                    {
                        release.Register(mutex.ReleaseMutex);
                        break;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Mutex exists but we don't have access to it
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    // Name conflict with another native handle
                }
                instanceId++;
            }
            
            return process.ProcessName + " (" + instanceId.ToString() + ")";
        }
    }
}
