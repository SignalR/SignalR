
namespace SignalR.Infrastructure
{
    public interface IPerformanceCounter
    {
        long Decrement();
        long Increment();
        long IncrementBy(long value);
        void NextSample();
        long RawValue { get; set; }
        void Close();
        void RemoveInstance();
    }
}
