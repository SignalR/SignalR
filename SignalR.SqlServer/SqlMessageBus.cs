using System.Threading.Tasks;

namespace SignalR.SqlServer
{
    public class SqlMessageBus : ScaleoutMessageBus
    {
        private readonly string _tableName = "[dbo].[SignalR_Messages]";
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
            _installer = sqlInstaller ?? new SqlInstaller(connectionString, _tableName);
            _installer.EnsureInstalled();

            _sender = sqlSender ?? new SqlSender(connectionString, _tableName);
            _receiver = sqlReceiver ?? new SqlReceiver(connectionString, _tableName, OnReceived);
        }

        protected override Task Send(Message[] messages)
        {
            return _sender.Send(messages);
        }

        public override void Dispose()
        {
            _receiver.Dispose();
            base.Dispose();
        }
    }
}
