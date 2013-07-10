using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public class VolatileSubscription : ISubscription
    {
        private Func<MessageResult, object, Task<bool>> callback;
        private int maxMessages;
        private object state;

        public VolatileSubscription(string identity, Func<MessageResult, object, Task<bool>> callback, int maxMessages, object state)
        {
            // TODO: Complete member initialization
            Identity = identity;
            callback = callback;
            maxMessages = maxMessages;
            state = state;
        }

        public string Identity
        {
            get;
            private set;
        }

        public bool SetQueued()
        {
            throw new NotImplementedException();
        }

        public bool UnsetQueued()
        {
            throw new NotImplementedException();
        }

        public Task Work()
        {
            
        }
    }
}
