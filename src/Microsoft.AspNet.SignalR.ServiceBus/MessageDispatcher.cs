// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNet.SignalR.Messaging;

    sealed class MessageDispatcher
    {
        readonly InputQueue<InternalMessage> inputQueue;
        readonly Func<string, ulong, Message[], Task> onReceivedAsync;

        public MessageDispatcher(InputQueue<InternalMessage> inputQueue, Func<string, ulong, Message[], Task> onReceivedAsync)
        {
            this.inputQueue = inputQueue;
            this.onReceivedAsync = onReceivedAsync;
        }

        public void Start()
        {
            new DispatchLoopAsyncResult(this, OnDispatchLoopCompleted, null).Start();
        }

        static void OnDispatchLoopCompleted(IAsyncResult asyncResult)
        {
            DispatchLoopAsyncResult.End(asyncResult);
        }

        sealed class DispatchLoopAsyncResult : IteratorAsyncResult<DispatchLoopAsyncResult>
        {
            readonly static Action<AsyncResult, Exception> CompletingAction = Finally;
            readonly MessageDispatcher owner;
            InternalMessage internalMessage;
            bool shouldContinue;

            public DispatchLoopAsyncResult(MessageDispatcher owner, AsyncCallback callback, object state)
                : base(TimeSpan.MaxValue, callback, state)
            {
                this.owner = owner;
                this.OnCompleting += CompletingAction;
            }

            protected override IEnumerator<AsyncStep> GetAsyncSteps()
            {
                this.shouldContinue = true;

                while (this.shouldContinue)
                {
                    this.internalMessage = null;

                    yield return this.CallAsync(
                        (thisPtr, t, c, s) => thisPtr.owner.inputQueue.BeginDequeue(TimeSpan.MaxValue, c, s),
                        (thisPtr, r) => thisPtr.internalMessage = thisPtr.owner.inputQueue.EndDequeue(r),
                        ExceptionPolicy.Continue);

                    if (this.LastAsyncStepException != null)
                    {
                        Log.MessageDispatcherDequeueException(this.LastAsyncStepException);
                    }

                    if (this.internalMessage == null)
                    {
                        break;
                    }

                    yield return this.CallAsync(
                        (thisPtr, t, c, s) =>
                        {
                            Task task = this.owner.onReceivedAsync(internalMessage.Stream, internalMessage.Id, internalMessage.Messages);
                            return new TaskAsyncResult(task, c, s);
                        },
                        (thisPtr, r) => TaskAsyncResult.End(r),
                        ExceptionPolicy.Continue);

                    if (this.LastAsyncStepException != null)
                    {
                        Log.MessageDispatcherErrorInCallback(this.LastAsyncStepException);
                    }
                }
            }

            static void Finally(AsyncResult asyncResult, Exception exception)
            {
                if (exception != null)
                {
                    Log.MessageDispatcherUnexpectedException(exception);
                }
            }
        }
    }
}
