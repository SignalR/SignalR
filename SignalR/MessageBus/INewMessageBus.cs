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
        Task Publish(string source, string eventKey, object value);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventKeys"></param>
        /// <param name="cursor"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        IDisposable Subscribe(ISubscriber subscriber, string cursor, Func<Exception, MessageResult, Task> callback);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        string GetCursor(string eventKey);
    }
}
