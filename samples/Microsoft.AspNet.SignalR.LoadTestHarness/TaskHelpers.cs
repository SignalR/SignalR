using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.AspNet.SignalR.LoadTestHarness
{
    public static class TaskHelpers
    {
        private readonly static TaskCompletionSource<object> _doneTcs = new TaskCompletionSource<object>();

        static TaskHelpers()
        {
            _doneTcs.SetResult(null);
        }

        public static Task Done
        {
            get
            {
                return _doneTcs.Task;
            }
        }
    }
}