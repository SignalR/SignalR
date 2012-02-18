using System;
using Microsoft.SPOT;

namespace System.Threading.Tasks
{
    public class Task
    {
        public delegate object TaskStart();
        public delegate object TaskContinuation(Task parent);

        public Task(TaskStart start)
        {
            Status = TaskStatus.Created;
            StartDelegate = () =>
            {
                try
                {
                    Result = start();
                    Status = TaskStatus.RanToCompletion;
                }
                catch (Exception ex)
                {
                    Exception = ex;
                    Status = TaskStatus.Faulted;
                }

                RunContinuation();
            };
        }

        protected ThreadStart StartDelegate { get; set; }

        protected Task Continuation { get; set; }

        public Exception Exception { get; private set; }

        public object Result { get; private set; }

        public TaskStatus Status { get; protected set; }

        public bool IsFaulted { get { return Status == TaskStatus.Faulted; } }

        public bool IsCompleted { get { return Status == TaskStatus.RanToCompletion; } }

        public virtual void Start()
        {
            var thread = new Thread(StartDelegate);
            thread.Start();
            
            Status = TaskStatus.Running;
        }

        public Task Then(TaskContinuation continuation)
        {
            Continuation = new Task(() => continuation(this));
            if (Status == TaskStatus.RanToCompletion)
            {
                // Task already completed
                RunContinuation();
            }
            return Continuation;
        }

        protected void RunContinuation()
        {
            if (Continuation == null)
            {
                return;
            }
            Continuation.Start();
        }

        private void Start(object result)
        {
            Result = result;
            Start();
        }
    }
}