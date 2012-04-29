using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gate;
using Owin;
using SignalR.Hubs;

namespace SignalR.Hosting.Owin
{
    public static class OwinHost
    {
        /// <summary>
        /// Add HubDispatcher to pipeline at default "/signalr" path
        /// </summary>
        public static IAppBuilder UseSignalRHubs(this IAppBuilder builder)
        {
            return builder.Map("/signalr", x => x.RunSignalR());
        }

        /// <summary>
        /// Add HubDispatcher to pipeline at default "/signalr" path
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="resolver">Used by components to acquire the services they depend on</param>
        /// <returns></returns>
        public static IAppBuilder UseSignalR(this IAppBuilder builder, IDependencyResolver resolver)
        {
            return builder.Map("/signalr", x => x.RunSignalR(resolver));
        }

        /// <summary>
        /// Add HubDispatcher to pipeline at user defined path
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="url">Base path for hub requests</param>
        /// <returns></returns>
        public static IAppBuilder UseSignalRHubs(this IAppBuilder builder, string url)
        {
            return builder.Map(url, x => x.RunSignalR());
        }

        /// <summary>
        /// Add HubDispatcher to pipeline at user defined path
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="url">Base path for hub requests</param>
        /// <param name="resolver">Used by components to acquire the services they depend on</param>
        /// <returns></returns>
        public static IAppBuilder UseSignalRHubs(this IAppBuilder builder, string url, IDependencyResolver resolver)
        {
            return builder.Map(url, x => x.RunSignalR(resolver));
        }

        /// <summary>
        /// Add a specific PersistentConnection type at user defined path
        /// </summary>
        /// <typeparam name="T">PersistentConnection type to expose</typeparam>
        /// <param name="builder"></param>
        /// <param name="url">Base url for persistent connection requests</param>
        /// <returns></returns>
        public static IAppBuilder UseSignalR<T>(this IAppBuilder builder, string url) where T : PersistentConnection
        {
            return builder.Map(url, x => x.RunSignalR<T>());
        }

        /// <summary>
        /// Add a specific PersistentConnection type at user defined path
        /// </summary>
        /// <typeparam name="T">PersistentConnection type to expose</typeparam>
        /// <param name="builder"></param>
        /// <param name="url">Base url for persistent connection requests</param>
        /// <param name="resolver">Used by components to acquire the services they depend on</param>
        /// <returns></returns>
        public static IAppBuilder UseSignalR<T>(this IAppBuilder builder, string url, IDependencyResolver resolver) where T : PersistentConnection
        {
            return builder.Map(url, x => x.RunSignalR<T>(resolver));
        }

        /// <summary>
        /// Sends all requests to HubDispatcher. RunSignalR should be used as the last item in a pipeline, or
        /// inside a Map statement.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IAppBuilder RunSignalR(this IAppBuilder builder)
        {
            return RunSignalR(builder, GlobalHost.DependencyResolver);
        }

        /// <summary>
        /// Sends all requests to HubDispatcher. RunSignalR should be used as the last item in a pipeline, or
        /// inside a Map statement.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="resolver">Used by components to acquire the services they depend on</param>
        /// <returns></returns>
        public static IAppBuilder RunSignalR(this IAppBuilder builder, IDependencyResolver resolver)
        {
            return builder.Use<AppDelegate>(_ => ExecuteConnection(env =>
            {
                var hubDispatcher = new HubDispatcher(env["owin.RequestPathBase"].ToString());
                hubDispatcher.Initialize(resolver);
                return hubDispatcher;
            }));
        }

        /// <summary>
        /// Sends all requests to a PersistentConnection type. RunSignalR should be used as the last item in a pipeline, or
        /// inside a Map statement.
        /// </summary>
        /// <typeparam name="T">PersistentConnection type to expose</typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IAppBuilder RunSignalR<T>(this IAppBuilder builder) where T : PersistentConnection
        {
            return RunSignalR<T>(builder, GlobalHost.DependencyResolver);
        }

        /// <summary>
        /// Sends all requests to a PersistentConnection type. RunSignalR should be used as the last item in a pipeline, or
        /// inside a Map statement.
        /// </summary>
        /// <typeparam name="T">PersistentConnection type to expose</typeparam>
        /// <param name="builder"></param>
        /// <param name="resolver">Used by components to acquire the services they depend on</param>
        /// <returns></returns>
        public static IAppBuilder RunSignalR<T>(this IAppBuilder builder, IDependencyResolver resolver) where T : PersistentConnection
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
                        var hostContext = new HostContext(request, response, Thread.CurrentPrincipal);

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
