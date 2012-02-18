using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SPOT;

namespace SignalR.Client.NetMF.Samples.Tasks
{
    public class Task : System.Threading.Tasks.Task
    {
        private readonly Dispatcher _dispatcher;

        public Task(TaskStart start, Dispatcher dispatcher)
            : base(start)
        {
            _dispatcher = dispatcher;
        }

        public override void Start()
        {
            if (_dispatcher != null)
            {
                _dispatcher.BeginInvoke(_ =>
                {
                    StartDelegate();
                    return true;
                }, null);
                Status = TaskStatus.Running;
            }
            else
            {
                base.Start();
            }
        }

        public System.Threading.Tasks.Task Then(TaskContinuation continuation, Dispatcher dispatcher)
        {
            Continuation = new Task(() => continuation(this), dispatcher);
            if (Status == TaskStatus.RanToCompletion)
            {
                // Task already completed
                RunContinuation();
            }
            return Continuation;
        }
    }
}