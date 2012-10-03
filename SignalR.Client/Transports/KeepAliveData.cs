using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SignalR.Client.Transports
{
    public class KeepAliveData
    {
        public KeepAliveData()
        {
            TimeoutCount = 2;
            TimeoutWarningThreshold = .66;
        }

        public TimeSpan KeepAlive
        {
            get;
            set
            {
                KeepAlive = value;
                // Calculate keep alive monitoring thresholds
                Timeout = TimeSpan.FromSeconds(TimeoutCount * KeepAlive.TotalSeconds);
                TimeoutWarning = TimeSpan.FromSeconds(Timeout.TotalSeconds * TimeoutWarningThreshold);
                KeepAliveCheckInterval = TimeSpan.FromSeconds((Timeout - TimeoutWarning).TotalSeconds / 3);
                WarningTriggered = false;
            }
        }

        /// <summary>
        /// After how many Keep Alives do we timeout a transport
        /// </summary>
        public int TimeoutCount
        {
            get;
            set;
        }

        /// <summary>
        /// Percentage of Timeout for when to warn the developer that transport may timeout/be slow
        /// </summary>
        public double TimeoutWarningThreshold
        {
            get;
            set;
        }

        /// <summary>
        /// When we will timeout a transport based on the keep alive
        /// </summary>
        public TimeSpan Timeout
        {
            get;
            private set;
        }
        
        /// <summary>
        /// When we will warn that a transport is being slow/may timeout
        /// </summary>
        public TimeSpan TimeoutWarning
        {
            get;
            private set;
        }

        /// <summary>
        /// How often a keep alive is checked
        /// </summary>
        public TimeSpan KeepAliveCheckInterval
        {
            get;
            private set;
        }

        public DateTime LastKeepAlive
        {
            get;
            set;
        }

        public bool WarningTriggered
        {
            get;
            set;
        }
    }
}
