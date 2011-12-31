namespace SignalR.Abstractions
{
    public static class HostConstants
    {
        /// <summary>
        /// The host should set this if they need to enable debug mode
        /// </summary>
        public static readonly string DebugMode = "debugMode";

        /// <summary>
        /// The host should set this is websockets can be supported
        /// </summary>
        public static readonly string SupportsWebSockets = "supportsWebSockets";
    }
}
