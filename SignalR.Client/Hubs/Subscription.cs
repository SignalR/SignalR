using System;
using Newtonsoft.Json.Linq;

namespace SignalR.Client.Hubs
{
    /// <summary>
    /// Represents a subscription to a hub method.
    /// </summary>
    public class Subscription
    {
        public event Action<JToken[]> Data;

        internal void OnData(JToken[] data)
        {
            if (Data != null)
            {
                Data(data);
            }
        } 
    }
}
