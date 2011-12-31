using Gate;

namespace SignalR.Owin
{
    public static class OwinExtensions
    {
        public static IAppBuilder MapConnection<T>(this IAppBuilder builder, string path) where T : PersistentConnection
        {
            return builder.Map(path, Delegates.ToDelegate(new OwinHost<T>().ProcessRequest));
        }
    }
}
