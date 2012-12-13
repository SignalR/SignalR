// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    public class SqlMessageBus : ScaleoutMessageBus
    {
        private readonly string _tableName = "SignalR_Messages";
        private readonly SqlInstaller _installer;
        private readonly SqlSender _sender;
        private readonly SqlReceiver _receiver;

        public SqlMessageBus(string connectionString, int tableCount, IDependencyResolver dependencyResolver)
            : this(connectionString, tableCount, null, null, null, dependencyResolver)
        {
            
        }

        internal SqlMessageBus(string connectionString, int tableCount, SqlInstaller sqlInstaller, SqlSender sqlSender, SqlReceiver sqlReceiver, IDependencyResolver dependencyResolver)
            : base(dependencyResolver)
        {
            _installer = sqlInstaller ?? new SqlInstaller(connectionString, _tableName, tableCount);
            _installer.EnsureInstalled();

            _sender = sqlSender ?? new SqlSender(connectionString, _tableName);
            _receiver = sqlReceiver ?? new SqlReceiver(connectionString, _tableName, OnReceived);
        }

        protected override Task Send(Message[] messages)
        {
            return _sender.Send(messages);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _receiver.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
