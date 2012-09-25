
namespace SignalR.Infrastructure
{
    internal class NoOpPerformanceCounter : IPerformanceCounter
    {
        public long Decrement()
        {
            return 0;
        }

        public long Increment()
        {
            return 0;
        }

        public long IncrementBy(long value)
        {
            return 0;
        }

        public long RawValue
        {
            get { return 0; }
            set { }
        }

        public void Close()
        {

        }

        public void RemoveInstance()
        {
            
        }

        public void NextSample()
        {
            
        }
    }
}
