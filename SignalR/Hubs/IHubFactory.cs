
namespace SignalR.Hubs {
    public interface IHubFactory {
        Hub CreateHub(string hubName);
    }
}