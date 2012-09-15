using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SignalR
{
    public class ScaleoutSubscription : Subscription
    {
        public ScaleoutSubscription()
            : base(null, null, null, 0, null)
        {

        }

        public override bool AddEvent(string key, Topic topic)
        {
            throw new NotImplementedException();
        }

        public override void RemoveEvent(string eventKey)
        {
            throw new NotImplementedException();
        }

        public override void SetEventTopic(string key, Topic topic)
        {
            throw new NotImplementedException();
        }

        public override string GetCursor()
        {
            throw new NotImplementedException();
        }

        protected override void PerformWork(ref List<ArraySegment<Message>> items, out string nextCursor, ref int totalCount, out object state)
        {
            throw new NotImplementedException();
        }
    }
}
