using System.Collections.Generic;

namespace SignalR.Hubs
{
    /// <summary>
    /// 
    /// </summary>
    public class ClientHubInvocation
    {
        /// <summary>
        /// 
        /// </summary>
        public string GroupOrConnectionId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Hub { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public object[] Args { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IDictionary<string, object> State { get; set; }
    }
}
