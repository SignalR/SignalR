using System.Threading.Tasks;

namespace SignalR
{
    public static class MessageBusExtensions
    {
        public static Task Publish(this INewMessageBus bus, string source, string key, string value)
        {
            return bus.Publish(new Message(source, key, value));
        }
    }
}
