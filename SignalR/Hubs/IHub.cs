
namespace SignalR.Hubs
{
    public interface IHub
    {
        IClientAgent Agent { get; set; }
        dynamic Caller { get; set; }
        dynamic Clients { get; }
        HubContext Context { get; set; }
        IGroupManager GroupManager { get; set; }
    }
}

