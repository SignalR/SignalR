﻿namespace SignalR.Hosting
{
    public static class HostConstants
    {
        /// <summary>
        /// The host should set this if they need to enable debug mode
        /// </summary>
        public static readonly string DebugMode = "debugMode";

        /// <summary>
        /// The host should set this is web sockets can be supported
        /// </summary>
        public static readonly string SupportsWebSockets = "supportsWebSockets";

        /// <summary>
        /// The host should set this if the web socket url is different
        /// </summary>
        public static readonly string WebSocketServerUrl = "webSocketServerUrl";

        public static readonly string ShutdownToken = "shutdownToken";
    }
}
