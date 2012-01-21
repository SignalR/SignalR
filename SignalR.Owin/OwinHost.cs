using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Gate.Owin;
using SignalR.Abstractions;
using SignalR.Infrastructure;

namespace SignalR.Hosting.Owin
{
    public static class OwinHost
    {
        public static IAppBuilder MapConnection<T>(this IAppBuilder builder) where T : PersistentConnection
        {
            return builder.Use<AppDelegate>(_ => ExecuteConnection(() =>
            {
                var factory = DependencyResolver.Resolve<IPersistentConnectionFactory>();
                return factory.CreateInstance(typeof(T));
            }));
        }

        private static AppDelegate ExecuteConnection(Func<PersistentConnection> factory)
        {
            return (environment, result, fault) =>
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
                        var response = new OwinResponse(result);
                        var hostContext = new HostContext(request, response, null);

                        try
                        {
                            PersistentConnection connection = factory();

                            connection
                                .ProcessRequestAsync(hostContext)
                                .ContinueWith(innerTask =>
                            {
                                if (innerTask.IsFaulted)
                                {
                                    fault(innerTask.Exception);
                                }
                                else
                                {
                                    response.End().Catch();
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            fault(ex);
                        }
                    }
                });
            };
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
            ex => tcs.TrySetException(ex),
            () => tcs.TrySetResult(text));

            return tcs.Task;
        }

        private static BodyDelegate GetRequestBodyDelegate(IDictionary<string, object> environment)
        {
            return (BodyDelegate)environment["owin.RequestBody"];
        }
    }
}
