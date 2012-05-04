using System;

namespace SignalR.Client.Hubs
{
    /// <summary>
    /// Represents a subscription to a hub method.
    /// </summary>
    public class Subscription
    {
        public event Action<object[]> Data;

        internal void OnData(object[] data)
        {
            if (Data != null)
            {
                Data(data);
            }
        } 
    }
}
