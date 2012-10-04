using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SignalR.Client.Transports
{
    /// <summary>
    /// Used to store Keep Alive information that is retrieved from server.
    /// </summary>
    public class KeepAliveData
    {
        /// <summary>
        /// Set default keep alive values
        /// </summary>
        public KeepAliveData()
        {
            TimeoutCount = 2;
            TimeoutWarningThreshold = .66;
        }

        /// <summary>
        /// Main accessor point of the KeepAliveData class.  When set the other members of the class are calculated and set.
        /// </summary>
        private TimeSpan _keepAlive;
        public TimeSpan KeepAlive
        {
            get
            {
                return _keepAlive;
            }
            set
            {
                _keepAlive = value;
                // Calculate keep alive monitoring thresholds
                Timeout = TimeSpan.FromSeconds(TimeoutCount * _keepAlive.TotalSeconds);
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

        /// <summary>
        /// Last time we received a keep alive from the server.
        /// </summary>
        public DateTime LastKeepAlive
        {
            get;
            set;
        }

        /// <summary>
        /// Flag that indicates whether we have warned the developer of a potential "slow" connection
        /// </summary>
        public bool WarningTriggered
        {
            get;
            set;
        }
    }
}
