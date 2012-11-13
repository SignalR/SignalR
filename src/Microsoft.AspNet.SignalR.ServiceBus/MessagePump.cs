// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Messaging;

    sealed class MessagePump
    {
        const int ReceiveBatchSize = 1000;
        static readonly TimeSpan BackoffAmount = TimeSpan.FromSeconds(20);

        readonly string id;
        readonly MessageReceiver receiver;
        readonly InputQueue<InternalMessage> inputQueue;
        readonly AsyncSemaphore semaphore;

        readonly Action exitSemaphore;

        public MessagePump(string id, MessageReceiver receiver, InputQueue<InternalMessage> inputQueue, AsyncSemaphore semaphore)
        {
            this.id = id;
            this.receiver = receiver;
            this.inputQueue = inputQueue;
            this.semaphore = semaphore;

            this.exitSemaphore = new Action(ExitSemaphore);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to ensure we catch all exceptions at this point.")]
        public void Start()
        {
            try
            {
                new PumpAsyncResult(this, OnPumpCompleted, this).Start();
            }
            catch (Exception e)
            {
                Environment.FailFast(e.ToString());
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to ensure we catch all exceptions at this point.")]
        static void OnPumpCompleted(IAsyncResult ar)
        {
            try
            {
                PumpAsyncResult.End(ar);
            }
            catch (Exception e)
            {
                Environment.FailFast(e.ToString());
            }
        }

        void ExitSemaphore()
        {
            this.semaphore.Exit();
        }

        sealed class PumpAsyncResult : IteratorAsyncResult<PumpAsyncResult>
        {
            static readonly Action<AsyncResult, Exception> CompletingAction = Finally;

            MessagePump owner;
            bool shouldContinuePump;

            // Variables reused in the loop.
            IEnumerable<BrokeredMessage> brokeredMessages;

            public PumpAsyncResult(MessagePump owner, AsyncCallback callback, object state)
                : base(TimeSpan.MaxValue, callback, state)
            {
                this.owner = owner;
                this.shouldContinuePump = true;

                this.OnCompleting += CompletingAction;
            }

            protected override IEnumerator<AsyncStep> GetAsyncSteps()
            {
                while (this.shouldContinuePump)
                {
                    // Reset status for the loop
                    this.brokeredMessages = null;

                    // Receives the message
                    yield return this.CallAsync(
                        (thisPtr, t, c, s) => thisPtr.owner.receiver.BeginReceiveBatch(ReceiveBatchSize, c, s),
                        (thisPtr, r) => thisPtr.brokeredMessages = thisPtr.owner.receiver.EndReceiveBatch(r),
                        ExceptionPolicy.Continue);

                    if (this.LastAsyncStepException != null)
                    {
                        Log.MessagePumpReceiveException(this.owner.id, this.LastAsyncStepException);

                        if (ShouldBackoff(this.LastAsyncStepException))
                        {
                            Log.MessagePumpBackoff(BackoffAmount, this.LastAsyncStepException);
                            yield return this.CallAsyncSleep(BackoffAmount);
                        }
                    }

                    // Retry next receive if no message was received.
                    if (this.brokeredMessages == null)
                    {
                        continue;
                    }

                    // Handles the message
                    foreach (BrokeredMessage brokeredMessage in this.brokeredMessages)
                    {
                        InternalMessage internalMessage = null;

                        try
                        {
                            ulong sequenceNumber = (ulong)brokeredMessage.SequenceNumber;
                            Message[] messages = MessageConverter.ToMessages(brokeredMessage);
                            internalMessage = new InternalMessage(this.owner.id, sequenceNumber, messages);
                        }
                        catch (Exception e)
                        {
                            Log.MessagePumpDeserializationException(e);
                        }
                        finally
                        {
                            brokeredMessage.Dispose();
                        }

                        if (internalMessage != null)
                        {
                            if (!this.owner.semaphore.TryEnter())
                            {
                                yield return this.CallAsync(
                                    (thisPtr, t, c, s) => thisPtr.owner.semaphore.BeginEnter(c, s),
                                    (thisPtr, r) => thisPtr.owner.semaphore.EndEnter(r),
                                    ExceptionPolicy.Transfer);
                            }

                            this.owner.inputQueue.EnqueueAndDispatch(internalMessage, this.owner.exitSemaphore, false);
                        }
                    }
                }
            }

            static bool ShouldBackoff(Exception exception)
            {
                if (exception is ServerBusyException)
                {
                    return true;
                }

                return false;
            }

            static void Finally(AsyncResult asyncResult, Exception exception)
            {
                if (exception != null)
                {
                    PumpAsyncResult thisPtr = (PumpAsyncResult)asyncResult;
                    Log.MessagePumpUnexpectedException(thisPtr.owner.id, exception);
                }
            }
        }
    }
}
