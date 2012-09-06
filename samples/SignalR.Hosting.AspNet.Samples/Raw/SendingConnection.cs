﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignalR.Hosting.AspNet.Samples
{
    public class SendingConnection : PersistentConnection
    {
        protected override Task OnReceivedAsync(IRequest request, string connectionId, string data)
        {            
            for (int i = 0; i < 10; i++)
            {
                Connection.Send(connectionId, String.Format("{0}{1}", data, i)).Wait();
            }

            var tcs = new TaskCompletionSource<object>();
            tcs.TrySetResult(null);
            return tcs.Task;
        }
    }
}