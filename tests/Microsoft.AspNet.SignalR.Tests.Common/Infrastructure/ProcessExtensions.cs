// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    public static class ProcessExtensions
    {
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "It's cleaned up when the cancellation token is disposed.")]
        public static string GetUniqueInstanceName(this Process process, CancellationToken release)
        {
            if (process == null)
            {
                throw new ArgumentNullException("process");
            }

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
                        release.Register(mutex.Dispose);
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

            return process.ProcessName + " (" + instanceId.ToString(CultureInfo.InvariantCulture) + ")";
        }
    }
}
