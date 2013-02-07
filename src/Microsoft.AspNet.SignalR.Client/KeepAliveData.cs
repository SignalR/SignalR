using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.AspNet.SignalR.Client
{
    /// <summary>
    /// Class to store all the Keep Alive properties
    /// </summary>
    public class KeepAliveData
    {
        public DateTime LastKeepAlive { get; set; }
        public TimeSpan Timeout { get; set; }
        public TimeSpan TimeoutWarning { get; set; }
        public TimeSpan CheckInterval { get; set; }
    }
}
