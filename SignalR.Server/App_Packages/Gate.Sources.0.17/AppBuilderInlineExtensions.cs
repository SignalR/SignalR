using System;
using System.Threading.Tasks;
using Owin;
using System.Collections.Generic;

namespace Gate
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    internal static class AppBuilderInlineExtensions
    {
        public static IAppBuilder UseGate(this IAppBuilder builder, Action<Request> app)
        {
            return builder.UseFunc(next => environment =>
            {
                app.Invoke(new Request(environment));
                return next(environment);
            });
        }

        public static IAppBuilder UseGate(this IAppBuilder builder, Action<Request, Response> app)
        {
            return builder.UseFunc(next => environment =>
            {
                app.Invoke(new Request(environment), new Response(environment));
                return TaskHelpers.Completed();
            });
        }

        public static IAppBuilder UseGate(this IAppBuilder builder, Func<Request, Task> app)
        {
            return builder.UseFunc(next => environment =>
                app.Invoke(new Request(environment)).Then(() => next(environment)));
        }

        public static IAppBuilder UseGate(this IAppBuilder builder, Func<Request, Response, Task> app)
        {
            return builder.UseFunc(_ => environment =>
                app.Invoke(new Request(environment), new Response(environment)));
        }

        public static IAppBuilder UseGate(this IAppBuilder builder, Func<Request, Response, Func<Task>, Task> app)
        {
            return builder.UseFunc(next => environment =>
                app.Invoke(new Request(environment), new Response(environment), () => next(environment)));
        }
    }
}
