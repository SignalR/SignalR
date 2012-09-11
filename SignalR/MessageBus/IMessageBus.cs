using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR
{
    public interface IMessageBus
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task Publish(Message message);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subscriber"></param>
        /// <param name="cursor"></param>
        /// <param name="callback"></param>
        /// <param name="maxMessages"></param>
        /// <returns></returns>
        IDisposable Subscribe(ISubscriber subscriber, string cursor, Func<MessageResult, Task<bool>> callback, int maxMessages);
    }
}
