using System.Threading.Tasks;

namespace SignalR
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class ScaleoutMessageBus : MessageBus
    {
        public ScaleoutMessageBus(IDependencyResolver resolver)
            : base(resolver)
        {
        }

        /// <summary>
        /// Initializes the backplane
        /// </summary>
        protected abstract void Initialize();

        /// <summary>
        /// Sends messages to the backplane
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        protected abstract Task Send(Message[] messages);

        /// <summary>
        /// Invoked when a payload is received from the backplane. There should only be one active call at any time.
        /// </summary>
        /// <param name="streamId">id of the stream</param>
        /// <param name="id">id of the payload within that stream</param>
        /// <param name="messages">List of messages associated</param>
        /// <returns></returns>
        protected Task<bool> OnReceived(string streamId, ulong id, Message[] messages)
        {
            return TaskAsyncHelper.True;
        }
    }
}
