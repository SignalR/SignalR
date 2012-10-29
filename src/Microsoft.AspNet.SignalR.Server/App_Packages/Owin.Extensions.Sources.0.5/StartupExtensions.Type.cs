using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Owin
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    internal static partial class StartupExtensions
    {
        public static IAppBuilder UseType<TMiddleware>(this IAppBuilder builder, params object[] args)
        {
            return builder.Use(typeof(TMiddleware), args);
        }

        public static IAppBuilder UseType(this IAppBuilder builder, Type type, params object[] args)
        {
            return builder.Use(type, args);
        }
    }
}
