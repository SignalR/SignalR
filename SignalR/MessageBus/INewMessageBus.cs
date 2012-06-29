using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR
{
    public interface INewMessageBus
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="eventKey"></param>
        /// <param name="value"></param>
        void Publish(string source, string eventKey, object value);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventKeys"></param>
        /// <param name="cursor"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        IDisposable Subscribe(IEnumerable<string> eventKeys, string cursor, Func<Exception, MessageResult, Task> callback);
    }
}
