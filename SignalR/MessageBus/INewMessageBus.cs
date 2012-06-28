using System;
using System.Collections.Generic;

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
        /// <param name="keys"></param>
        /// <param name="messageId"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        IDisposable Subscribe(IEnumerable<string> keys, string messageId, Action<Exception, MessageResult> callback);
    }
}
