using System;
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
            Initialize();
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
            // { 0, 0, ({foo, 1}, {bar,2}) }

            // foo -> [1]
            // bar -> [2]

            // { 0, 1, ({foo, 2}, {bar,3}, {foo, 10}) }

            // foo -> [1, 2, 10]
            // bar -> [2, 3]

            // { 0, 2, ({foo, 3}, {bar,4}, {baz, hi}) }

            // foo -> [1, 2, 10, 3]
            // bar -> [2, 3, 4]
            // baz -> [hi]

            // { 0, 0, (foo, 0), (bar, 0) }
            // { 0, 1, (foo, 1), (bar, 1), (foo, 2) }
            // { 0, 2, (foo, 3), (bar, 2), (baz, 0) }


            // cursor = null = (foo, 0), (bar, 0)
            // cursor = (0, 0)

            // subscribe((foo, bar, baz), (0, 0))

            return TaskAsyncHelper.True;
        }

        public override Task Publish(Message message)
        {
            // TODO: Buffer messages here and make it configurable
            return Send(new[] { message });
        }

        public override IDisposable Subscribe(ISubscriber subscriber, string cursor, Func<MessageResult, Task<bool>> callback, int messageBufferSize)
        {
            // The format of the cursor is (sid, pid, localid)

            return base.Subscribe(subscriber, cursor, callback, messageBufferSize);
        }
    }
}
