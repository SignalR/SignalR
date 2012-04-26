namespace SignalR.Hubs
{
    public interface IHub
    {
        IClientAgent Agent { get; set; }
        dynamic Caller { get; set; }
        HubContext Context { get; set; }
        IGroupManager Groups { get; set; }
    }
}

