// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;

namespace Microsoft.AspNet.SignalR.SqlServer
{
    /// <summary>
    /// Uses SQL Server tables to scale-out SignalR applications in web farms.
    /// </summary>
    public class SqlMessageBus : ScaleoutMessageBus
    {
        internal const string SchemaName = "SignalR";

        private const int DefaultBufferSize = 1000;
        private const string _tableNamePrefix = "Messages";
        
        private readonly string _connectionString;
        private readonly int _tableCount;
        private readonly TraceSource _trace;
        
        private SqlStream[] _streams;

        /// <summary>
        /// Creates a new instance of the SqlMessageBus class.
        /// </summary>
        /// <param name="connectionString">The SQL Server connection string.</param>
        /// <param name="tableCount">The number of tables to use as "message tables".</param>
        /// <param name="dependencyResolver">The dependency resolver.</param>
        public SqlMessageBus(string connectionString, int tableCount, IDependencyResolver dependencyResolver)
            : this(connectionString, tableCount, null, dependencyResolver)
        {

        }

        internal SqlMessageBus(string connectionString, int tableCount, SqlInstaller sqlInstaller, IDependencyResolver dependencyResolver)
            : base(dependencyResolver)
        {
            if (String.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException("connectionString");
            }

            if (tableCount < 1)
            {
                throw new ArgumentOutOfRangeException("tableCount", String.Format(CultureInfo.InvariantCulture, Resources.Error_ValueMustBeGreaterThan1, "tableCount"));
            }

            _connectionString = connectionString;
            _tableCount = tableCount;

            var traceManager = dependencyResolver.Resolve<ITraceManager>();
            _trace = traceManager["SignalR." + typeof(SqlMessageBus).Name];

            ReconnectDelay = TimeSpan.FromSeconds(3);

            ThreadPool.QueueUserWorkItem(Initialize);
        }

        public TimeSpan ReconnectDelay { get; set; }

        protected override int StreamCount
        {
            get
            {
                return _tableCount;
            }
        }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            return _streams[streamIndex].Send(messages);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (var i = 0; i < _streams.Length; i++)
                {
                    _streams[i].Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private void Initialize(object state)
        {
            // NOTE: Called from ThreadPool thread
            try
            {
                var installer = new SqlInstaller(_connectionString, _tableNamePrefix, _tableCount, _trace);
                installer.Install();

                _streams = Enumerable.Range(0, _tableCount)
                                     .Select(streamIndex => new SqlStream(streamIndex, _connectionString,
                                         String.Format(CultureInfo.InvariantCulture, "{0}_{1}", _tableNamePrefix, streamIndex),
                                         () => Buffer(streamIndex, DefaultBufferSize), OnReceived, _trace))
                                     .ToArray();

                Open();
            }
            catch (Exception ex)
            {
                // Fatal exception while initializing, Close and call the error callback
                Close(ex);
                
                // TODO: Invoke user defined error callback

            }
        }
    }
}
