using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Owin;
using SignalR.Hosting;
using SignalR.Hubs;
using SignalR.Infrastructure;

namespace SignalR.Hosting.Owin
{
    public static class OwinHost
    {
        public static IAppBuilder RunSignalR(this IAppBuilder builder)
        {
            return RunSignalR(builder, new DefaultDependencyResolver());
        }

        public static IAppBuilder RunSignalR(this IAppBuilder builder, IDependencyResolver resolver)
        {
            return builder.Use<AppDelegate>(_ => ExecuteConnection(env =>
            {
                var hubDispatcher = new HubDispatcher(env["owin.RequestPathBase"].ToString());
                hubDispatcher.Initialize(resolver);
                return hubDispatcher;
            }));
        }

        public static IAppBuilder RunConnection<T>(this IAppBuilder builder) where T : PersistentConnection
        {
            return RunConnection<T>(builder, new DefaultDependencyResolver());
        }

        public static IAppBuilder RunConnection<T>(this IAppBuilder builder, IDependencyResolver resolver) where T : PersistentConnection
        {
            return builder.Use<AppDelegate>(_ => ExecuteConnection(env =>
            {
                var factory = new PersistentConnectionFactory(resolver);
                var connection = factory.CreateInstance(typeof(T));
                connection.Initialize(resolver);
                return connection;
            }));
        }

        private static AppDelegate ExecuteConnection(Func<IDictionary<string,object>, PersistentConnection> factory)
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
                            PersistentConnection connection = factory(environment);

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

            requestBodyDelegate.Invoke(
                data =>
                {
                    // TODO: Check the continuation and read async if it isn't null
                    text += Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);
                    return false;
                },
                _ => false,
                ex =>
                {
                    if (ex == null)
                        tcs.TrySetResult(text);
                    else
                        tcs.TrySetException(ex);
                },
                CancellationToken.None);

            return tcs.Task;
        }

        private static BodyDelegate GetRequestBodyDelegate(IDictionary<string, object> environment)
        {
            return (BodyDelegate)environment["owin.RequestBody"];
        }
    }
}
