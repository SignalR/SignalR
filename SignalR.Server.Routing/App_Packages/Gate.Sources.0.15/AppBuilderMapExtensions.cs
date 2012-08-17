using System;
using Gate.Mapping;
using Owin;

namespace Gate
{
    internal static class AppBuilderMapExtensions
    {
        /*
         * Fundamental definition of Map.
         */

        public static IAppBuilder Map(this IAppBuilder builder, string path, AppDelegate app)
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
            return builder.Map(path, builder.BuildNew<AppDelegate>(x=>app(x)));
        }

        /*
         * Extensions to map AppDelegate factory func to a given path, with optional parameters.
         */

        public static IAppBuilder Map(this IAppBuilder builder, string path, object app)
        {
            return builder.Map(path, b2 => b2.Run(app));
        }
    }
}