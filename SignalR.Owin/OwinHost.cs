using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SignalR.Abstractions;
using SignalR.Infrastructure;

namespace SignalR.Owin
{
    using BodyDelegate = System.Func<System.Func<System.ArraySegment<byte>, // data
                                     System.Action,                         // continuation
                                     bool>,                                 // continuation will be invoked
                                     System.Action<System.Exception>,       // onError
                                     System.Action,                         // on Complete
                                     System.Action>;

    using ResponseCallBack = System.Action<string, System.Collections.Generic.IDictionary<string, string>, System.Func<System.Func<System.ArraySegment<byte>, System.Action, bool>, System.Action<System.Exception>, System.Action, System.Action>>; 

    public class OwinHost<T>
    {
        public void ProcessRequest(IDictionary<string, object> environment, ResponseCallBack responseCallback, Action<Exception> fault)
        {
            // Read the request body then process the request.
            ParseBodyAsync(environment).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    // There was an error reading the body
                    fault(task.Exception);
                }
                else
                {
                    var request = new OwinRequest(environment, task.Result);
                    var response = new OwinResponse(responseCallback);
                    var hostContext = new HostContext(request, response, null);

                    try
                    {
                        var factory = DependencyResolver.Resolve<IPersistentConnectionFactory>();
                        PersistentConnection connection = factory.CreateInstance(typeof(T));

                        connection.ProcessRequestAsync(hostContext).ContinueWith(innerTask =>
                        {
                            fault(innerTask.Exception);
                        },
                        TaskContinuationOptions.OnlyOnFaulted);
                    }
                    catch (Exception ex)
                    {
                        fault(ex);
                    }
                }
            });
        }

        private static Task<string> ParseBodyAsync(IDictionary<string, object> environment)
        {
            var requestBodyDelegate = GetRequestBodyDelegate(environment);

            var tcs = new TaskCompletionSource<string>();
            if (requestBodyDelegate == null)
            {
                tcs.SetResult(null);
                return tcs.Task;
            }

            string text = null;

            requestBodyDelegate.Invoke((data, continuation) =>
            {
                // TODO: Check the continuation and read async if it isn't null
                text = Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);
                return false;
            },
            tcs.SetException,
            () => tcs.SetResult(text));

            return tcs.Task;
        }

        private static BodyDelegate GetRequestBodyDelegate(IDictionary<string, object> environment)
        {
            return (BodyDelegate)environment["owin.RequestBody"];
        }
    }
}
