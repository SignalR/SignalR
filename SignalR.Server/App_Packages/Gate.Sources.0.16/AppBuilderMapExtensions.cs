using System;
using Gate.Mapping;
using System.Threading.Tasks;
using Owin;
using System.Collections.Generic;

namespace Gate
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    internal static class AppBuilderMapExtensions
    {
        /*
         * Fundamental definition of Map.
         */

        public static IAppBuilder Map(this IAppBuilder builder, string path, AppFunc app)
        {
            var mapBuilder = builder as MapBuilder ?? new MapBuilder(builder, UrlMapper.Create);
            mapBuilder.MapInternal(path, app);
            return mapBuilder;
        }

        /*
         * Extension to allow branching of AppBuilder.
         */

        public static IAppBuilder Map(this IAppBuilder builder, string path, Action<IAppBuilder> app)
        {
            return builder.Map(path, builder.BuildNew<AppFunc>(x => app(x)));
        }

        /*
         * Extensions to map AppFunc factory func to a given path, with optional parameters.
         */

        public static IAppBuilder Map(this IAppBuilder builder, string path, object app)
        {
            return builder.Map(path, b2 => b2.Run(app));
        }
    }
}