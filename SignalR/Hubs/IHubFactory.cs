
namespace SignalR.Hubs
{
    public interface IHubFactory
    {
        IHub CreateHub(string hubName);
    }
}