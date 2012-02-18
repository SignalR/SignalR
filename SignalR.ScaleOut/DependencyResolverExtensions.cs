using SignalR.Infrastructure;

namespace SignalR.ScaleOut
{
    public static class DependencyResolverExtensions
    {
        public static IDependencyResolver EnableSqlScaleOut(this IDependencyResolver dependencyResolver, string connectionString)
        {
            //dependencyResolver.RegisterSqlMessageStore(connectionString);
            //dependencyResolver.RegisterSqlSignalBus(connectionString);
            return dependencyResolver;
        }

        public static IDependencyResolver RegisterSqlMessageStore(this IDependencyResolver dependencyResolver, string connectionString)
        {
            //var store = new SQLMessageStore(connectionString);
            //dependencyResolver.Register(typeof(IMessageStore), () => store);
            return dependencyResolver;
        }

        public static IDependencyResolver RegisterSqlSignalBus(this IDependencyResolver dependencyResolver, string connectionString)
        {
            //var signalBus = new SQLQueryNotificationsSignalBus(connectionString);
            //dependencyResolver.Register(typeof(ISignalBus), () => signalBus);
            return dependencyResolver;
        }
    }
}