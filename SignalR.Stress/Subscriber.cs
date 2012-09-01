using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SignalR.Stress
{
    public class Subscriber : ISubscriber
    {
        public Subscriber(string id, IEnumerable<string> eventKeys)
        {
            Identity = id;
            EventKeys = eventKeys;
        }

        public IEnumerable<string> EventKeys
        {
            get;
            set;
        }

        public event Action<string, string> EventAdded;

        public event Action<string> EventRemoved;

        public string Identity
        {
            get;
            private set;
        }
    }
}
