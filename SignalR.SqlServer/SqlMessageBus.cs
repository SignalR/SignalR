using System.Threading.Tasks;

namespace SignalR.SqlServer
{
    public class SqlMessageBus : ScaleoutMessageBus
    {
        private readonly string _tableName = "[dbo].[SignalR_Messages]";
        private readonly IDependencyResolver _dependencyResolver;
        private readonly SqlInstaller _installer;
        private readonly SqlSender _sender;
        private readonly SqlReceiver _receiver;

        public SqlMessageBus(string connectionString, IDependencyResolver dependencyResolver)
            : this(connectionString, null, null, null, dependencyResolver)
        {
            
        }

        internal SqlMessageBus(string connectionString, SqlInstaller sqlInstaller, SqlSender sqlSender, SqlReceiver sqlReceiver, IDependencyResolver dependencyResolver)
            : base(dependencyResolver)
        {
            _dependencyResolver = dependencyResolver;
            
            _installer = sqlInstaller ?? new SqlInstaller(connectionString, _tableName);
            _installer.EnsureInstalled();

            var json = _dependencyResolver.Resolve<IJsonSerializer>();
            _sender = sqlSender ?? new SqlSender(connectionString, _tableName, json);
            _receiver = sqlReceiver ?? new SqlReceiver(connectionString, _tableName, OnReceived, json);
        }

        protected override Task Send(Message[] messages)
        {
            return _sender.Send(messages);
        }
    }
}
