namespace SignalR.ServiceBus
{
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;

    public sealed class TopicMessageBus
    {
        const int MaxInputQueueLength = 10000;

        readonly string prefix;
        readonly bool isAnonymous;
        readonly string connectionString;
        readonly int partitionCount;
        readonly int nodeCount;
        readonly int nodeId;

        readonly Func<string, ulong, Message[], Task> onReceivedAsync;

        readonly Dictionary<int, MessagingFactory> factories;
        readonly Dictionary<int, MessagePump> pumps;
        readonly Dictionary<int, MessageSender> senders;

        readonly AsyncSemaphore semaphore;
        readonly InputQueue<InternalMessage> inputQueue;

        readonly Task<bool> initializeTask;

        NamespaceManager namespaceManager;
        MessageDispatcher dispatcher;

        public TopicMessageBus(
            string connectionString,
            int partitionCount,
            int nodeCount,
            int nodeId,
            string prefix,
            bool isAnonymous,
            Func<string, ulong, Message[], Task> onReceivedAsync)
        {
            this.prefix = prefix;
            this.isAnonymous = isAnonymous;
            this.connectionString = connectionString;
            this.partitionCount = partitionCount;
            this.nodeCount = nodeCount;
            this.nodeId = nodeId;

            this.factories = new Dictionary<int, MessagingFactory>();
            this.pumps = new Dictionary<int, MessagePump>();
            this.senders = new Dictionary<int, MessageSender>();
            this.onReceivedAsync = onReceivedAsync;

            this.semaphore = new AsyncSemaphore(MaxInputQueueLength);
            this.inputQueue = new InputQueue<InternalMessage>();

            this.initializeTask = Task.Factory.FromAsync(
                (c, s) => new InitializeAsyncResult(this, c, s).Start(),
                r => { InitializeAsyncResult.End(r); return true; },
                null);
        }

        public void Start()
        {
        }

        string GetTopicPath(int partitionId)
        {
            return this.prefix + partitionId.ToString(NumberFormatInfo.InvariantInfo);
        }

        static string GetSubscriptionName(int nodeId)
        {
            return nodeId.ToString(NumberFormatInfo.InvariantInfo);
        }

        public Task SendAsync(Message[] messages)
        {
            try
            {
                return Task.Factory.FromAsync(this.BeginSend, this.EndSend, messages, null);
            }
            catch (Exception exception)
            {
                TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
                completionSource.SetException(exception);
                return completionSource.Task;
            }
        }

        IAsyncResult BeginSend(Message[] messages, AsyncCallback callback, object state)
        {
            return new SendAsyncResult(this, messages, callback, state).Start();
        }

        void EndSend(IAsyncResult asyncResult)
        {
            SendAsyncResult.End(asyncResult);
        }

        sealed class InitializeAsyncResult : IteratorAsyncResult<InitializeAsyncResult>
        {
            readonly ServiceBusFactory serviceBusFactory;
            readonly static Action<AsyncResult, Exception> CompletingAction = Finally;
            readonly TopicMessageBus owner;

            public InitializeAsyncResult(TopicMessageBus owner, AsyncCallback callback, object state)
                : base(TimeSpan.MaxValue, callback, state)
            {
                this.owner = owner;
                this.serviceBusFactory = new ServiceBusFactory(this.owner.connectionString);
                this.OnCompleting += CompletingAction;
            }

            protected override IEnumerator<AsyncStep> GetAsyncSteps()
            {
                this.owner.namespaceManager = this.serviceBusFactory.CreateNamespaceManager();

                for (int partitionId = 0; partitionId < this.owner.partitionCount; partitionId++)
                {
                    // Create topic if not exists.
                    string topicPath = this.owner.GetTopicPath(partitionId);

                    bool topicExist = false;

                    yield return this.CallAsync(
                        (thisPtr, t, c, s) => thisPtr.owner.namespaceManager.BeginTopicExists(topicPath, c, s),
                        (thisPtr, r) => topicExist = thisPtr.owner.namespaceManager.EndTopicExists(r),
                        ExceptionPolicy.Transfer);

                    if (!topicExist)
                    {
                        TopicDescription topicDescription = new TopicDescription(topicPath);

                        yield return this.CallAsync(
                            (thisPtr, t, c, s) => thisPtr.owner.namespaceManager.BeginCreateTopic(topicDescription, c, s),
                            (thisPtr, r) => topicDescription = thisPtr.owner.namespaceManager.EndCreateTopic(r),
                            ExceptionPolicy.Transfer);
                    }

                    for (int nodeId = 0; nodeId < this.owner.nodeCount; nodeId++)
                    {
                        // Create subscriptions if not exist
                        string subscriptionName = GetSubscriptionName(nodeId);

                        bool subscriptionExists = false;

                        yield return this.CallAsync(
                            (thisPtr, t, c, s) => thisPtr.owner.namespaceManager.BeginSubscriptionExists(topicPath, subscriptionName, c, s),
                            (thisPtr, r) => subscriptionExists = thisPtr.owner.namespaceManager.EndSubscriptionExists(r),
                            ExceptionPolicy.Transfer);

                        if (!subscriptionExists)
                        {
                            SubscriptionDescription subscriptionDescription = new SubscriptionDescription(topicPath, subscriptionName)
                            {
                                RequiresSession = false
                            };

                            yield return this.CallAsync(
                                (thisPtr, t, c, s) => thisPtr.owner.namespaceManager.BeginCreateSubscription(subscriptionDescription, c, s),
                                (thisPtr, r) => thisPtr.owner.namespaceManager.EndCreateSubscription(r),
                                ExceptionPolicy.Transfer);
                        }
                    }
                }

                for (int partitionId = 0; partitionId < this.owner.partitionCount; partitionId++)
                {
                    MessagingFactory factory = null;

                    yield return this.CallAsync(
                        (thisPtr, t, c, s) => thisPtr.serviceBusFactory.BeginCreateMessagingFactory(c, s),
                        (thisPtr, r) => factory = thisPtr.serviceBusFactory.EndCreateMessagingFactory(r),
                        ExceptionPolicy.Transfer);

                    this.owner.factories.Add(partitionId, factory);

                    string topicPath = this.owner.GetTopicPath(partitionId);
                    string subscriptionName = GetSubscriptionName(this.owner.nodeId);
                    string subscriptionEntityPath = SubscriptionClient.FormatSubscriptionPath(topicPath, subscriptionName);
                    
                    MessageSender sender = null;

                    yield return this.CallAsync(
                        (thisPtr, t, c, s) => factory.BeginCreateMessageSender(topicPath, c, s),
                        (thisPtr, r) => sender = factory.EndCreateMessageSender(r),
                        ExceptionPolicy.Transfer);

                    this.owner.senders.Add(partitionId, sender);

                    MessageReceiver receiver = null;
                    
                    yield return this.CallAsync(
                        (thisPtr, t, c, s) => factory.BeginCreateMessageReceiver(subscriptionEntityPath, ReceiveMode.ReceiveAndDelete, c, s),
                        (thisPtr, r) => receiver = factory.EndCreateMessageReceiver(r),
                        ExceptionPolicy.Transfer);

                    var pump = new MessagePump(topicPath, receiver, this.owner.inputQueue, this.owner.semaphore);
                    this.owner.pumps.Add(partitionId, pump);

                    pump.Start();
                }

                this.owner.dispatcher = new MessageDispatcher(this.owner.inputQueue, this.owner.onReceivedAsync);
                this.owner.dispatcher.Start();
            }

            static void Finally(AsyncResult asyncResult, Exception exception)
            {
                if (exception != null)
                {
                    Log.TopicMessagePumpInitializationFailed(exception);
                }
            }
        }

        sealed class SendAsyncResult : IteratorAsyncResult<SendAsyncResult>
        {
            readonly Action<AsyncResult, Exception> CompletingAction = Finally;
            readonly TopicMessageBus owner;
            readonly Message[] messages;
            Dictionary<int, List<Message>> partitionedMessages;

            public SendAsyncResult(TopicMessageBus owner, Message[] messages, AsyncCallback callback, object state)
                : base(TimeSpan.MaxValue, callback, state)
            {
                this.owner = owner;
                this.messages = messages;
                this.partitionedMessages = new Dictionary<int, List<Message>>();

                this.OnCompleting += CompletingAction;
            }

            int GetPartitionId(Message message)
            {
                return Md5Hash.Compute32bitHashCode(message.Source) % this.owner.partitionCount;
            }

            protected override IEnumerator<AsyncStep> GetAsyncSteps()
            {
                if (this.owner.initializeTask.IsFaulted)
                {
                    throw this.owner.initializeTask.Exception.GetBaseException();
                }

                if (!this.owner.initializeTask.IsCompleted)
                {
                    yield return this.CallAsync(
                        (thisPtr, t, c, s) => new TaskAsyncResult(this.owner.initializeTask, c, s),
                        (thisPtr, r) => TaskAsyncResult.End(r),
                        ExceptionPolicy.Transfer);
                }

                foreach (Message message in this.messages)
                {
                    int partitionId = GetPartitionId(message);

                    List<Message> messageList;
                    if (!this.partitionedMessages.TryGetValue(partitionId, out messageList))
                    {
                        messageList = new List<Message>();
                        this.partitionedMessages.Add(partitionId, messageList);
                    }

                    messageList.Add(message);
                }

                yield return this.CallParallelAsync(
                    partitionedMessages,
                    (thisPtr, i, t, c, s) => thisPtr.owner.senders[i.Key].BeginSend(MessageConverter.ToBrokeredMessages(i.Value), c, s),
                    (thisPtr, i, r) => thisPtr.owner.senders[i.Key].EndSend(r),
                    ExceptionPolicy.Transfer);
            }

            static void Finally(AsyncResult result, Exception exception)
            {
                if (exception != null)
                {
                    Log.TopicMessageBusSendFailure(exception);
                }
            }
        }
    }
}
