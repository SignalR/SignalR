
namespace SignalR.Infrastructure
{
    public interface IPerformanceCounterWriter
    {
        void Initialize(HostContext hostContext);

        void Decrement(string counterName);
        void Increment(string counterName);
        void IncrementBy(string counterName, long value);
    }
}
