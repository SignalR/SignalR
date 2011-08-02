namespace SignalR.Client.Hubs {
    public class HubInvocationInfo {
        public string Hub { get; set; }
        public string Method { get; set; }
        public object[] Args { get; set; }
    }
}
