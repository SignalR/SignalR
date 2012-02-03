namespace SignalR.MessageBus
{
    public class InMemoryMessage : Message
    {
        public ulong Id { get; private set; }
        public InMemoryMessage(string key, object value, ulong id)
            : base(key, value)
        {
            Id = id;
        }
    }
}
