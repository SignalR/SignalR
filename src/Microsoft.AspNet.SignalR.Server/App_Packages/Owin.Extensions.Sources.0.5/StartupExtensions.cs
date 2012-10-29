using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Owin
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    internal static partial class StartupExtensions
    {
        public static void Run(this IAppBuilder builder, object app)
        {
            builder.Use(new Func<object, object>(ignored => app));
        }

        public static AppFunc Build(this IAppBuilder builder)
        {
            return builder.Build<AppFunc>();
        }

        public static TApp Build<TApp>(this IAppBuilder builder)
        {
            return (TApp)builder.Build(typeof(TApp));
        }

        public static AppFunc BuildNew(
           this IAppBuilder builder,
           Action<IAppBuilder> configuration)
        {
            return builder.BuildNew<AppFunc>(configuration);
        }

        public static TApp BuildNew<TApp>(
           this IAppBuilder builder,
           Action<IAppBuilder> configuration)
        {
            var nested = builder.New();
            configuration(nested);
            return nested.Build<TApp>();
        }

        public static void AddSignatureConversion(
            this IAppBuilder builder,
            Delegate conversion)
        {
            object value;
            if (builder.Properties.TryGetValue("builder.AddSignatureConversion", out value) &&
                value is Action<Delegate>)
            {
                ((Action<Delegate>)value).Invoke(conversion);
            }
            else
            {
                throw new InvalidOperationException("IAppBuilder does not contain builder.AddSignatureConversion method");
            }
        }
    }
}
