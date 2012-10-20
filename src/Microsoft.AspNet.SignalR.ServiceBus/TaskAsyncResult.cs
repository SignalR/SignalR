// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System;
    using System.Threading.Tasks;

    sealed class TaskAsyncResult : AsyncResult<TaskAsyncResult>
    {
        readonly Task task;

        public TaskAsyncResult(Task task, AsyncCallback callback, object state)
            : base(callback, state)
        {
            this.task = task;
            if (this.task.IsCompleted)
            {
                this.CompleteWithTaskResult();
            }
            else
            {
                this.task.ContinueWith(this.OnTaskContinued);
            }
        }

        void OnTaskContinued(Task unused)
        {
            this.CompleteWithTaskResult();
        }

        void CompleteWithTaskResult()
        {
            if (this.task.Exception != null)
            {
                this.Complete(false, this.task.Exception.GetBaseException());
            }
            else
            {
                this.Complete(false);
            }
        }
    }
}
