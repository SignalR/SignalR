using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Owin
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    internal static partial class StartupExtensions
    {
        public static IAppBuilder UseFunc<TApp>(this IAppBuilder builder, Func<TApp, TApp> middleware)
        {
            return builder.Use(middleware);
        }

        public static IAppBuilder UseFunc(this IAppBuilder builder, Func<AppFunc, AppFunc> middleware)
        {
            return builder.Use(middleware);
        }

        public static IAppBuilder UseFunc<T1>(this IAppBuilder builder, Func<AppFunc, T1, AppFunc> middleware, T1 arg1)
        {
            return builder.UseFunc<AppFunc>(app => middleware(app, arg1));
        }

        public static IAppBuilder UseFunc<T1, T2>(this IAppBuilder builder, Func<AppFunc, T1, T2, AppFunc> middleware, T1 arg1, T2 arg2)
        {
            return builder.UseFunc<AppFunc>(app => middleware(app, arg1, arg2));
        }

        public static IAppBuilder UseFunc<T1, T2, T3>(this IAppBuilder builder, Func<AppFunc, T1, T2, T3, AppFunc> middleware, T1 arg1, T2 arg2, T3 arg3)
        {
            return builder.UseFunc<AppFunc>(app => middleware(app, arg1, arg2, arg3));
        }

        public static IAppBuilder UseFunc<T1, T2, T3, T4>(this IAppBuilder builder, Func<AppFunc, T1, T2, T3, T4, AppFunc> middleware, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return builder.UseFunc<AppFunc>(app => middleware(app, arg1, arg2, arg3, arg4));
        }

        public static IAppBuilder UseFunc<T1>(this IAppBuilder builder, Func<T1, Func<AppFunc, AppFunc>> middleware, T1 arg1)
        {
            return builder.UseFunc<AppFunc>(app => middleware(arg1)(app));
        }

        public static IAppBuilder UseFunc<T1, T2>(this IAppBuilder builder, Func<T1, T2, Func<AppFunc, AppFunc>> middleware, T1 arg1, T2 arg2)
        {
            return builder.UseFunc<AppFunc>(app => middleware(arg1, arg2)(app));
        }

        public static IAppBuilder UseFunc<T1, T2, T3>(this IAppBuilder builder, Func<T1, T2, T3, Func<AppFunc, AppFunc>> middleware, T1 arg1, T2 arg2, T3 arg3)
        {
            return builder.UseFunc<AppFunc>(app => middleware(arg1, arg2, arg3)(app));
        }

        public static IAppBuilder UseFunc<T1, T2, T3, T4>(this IAppBuilder builder, Func<T1, T2, T3, T4, Func<AppFunc, AppFunc>> middleware, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return builder.UseFunc<AppFunc>(app => middleware(arg1, arg2, arg3, arg4)(app));
        }
    }
}
