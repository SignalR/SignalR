﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
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
        private readonly List<SqlStream> _streams = new List<SqlStream>();

        /// <summary>
        /// Creates a new instance of the SqlMessageBus class.
        /// </summary>
        /// <param name="connectionString">The SQL Server connection string.</param>
        /// <param name="tableCount">The number of tables to use as "message tables".</param>
        /// <param name="dependencyResolver">The dependency resolver.</param>
        public SqlMessageBus(string connectionString, int tableCount, IDependencyResolver dependencyResolver)
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

            ThreadPool.QueueUserWorkItem(Initialize);
        }

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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Disposing")]
        protected override void Dispose(bool disposing)
        {
            for (var i = 0; i < _streams.Count; i++)
            {
                _streams[i].Dispose();
            }

            base.Dispose(disposing);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "On a background thread and we report exceptions asynchronously")]
        private void Initialize(object state)
        {
            // NOTE: Called from ThreadPool thread
            try
            {
                var installer = new SqlInstaller(_connectionString, _tableNamePrefix, _tableCount, _trace);
                installer.Install();
            }
            catch (Exception ex)
            {
                // Fatal exception while initializing, Close and call the error callback
                for (var i = 0; i < _tableCount; i++)
                {
                    Close(i, ex); 
                }
                
                // TODO: Invoke user defined error callback

                return;
            }

            for (var i = 0; i < _tableCount; i++)
            {
                var streamIndex = i;

                var stream = new SqlStream(streamIndex, _connectionString,
                    tableName: String.Format(CultureInfo.InvariantCulture, "{0}_{1}", _tableNamePrefix, streamIndex),
                    onReceived: OnReceived,
                    onRetry: () => Buffer(streamIndex, DefaultBufferSize),
                    onError: ex => Close(streamIndex, ex),
                    traceSource: _trace);
                    
                _streams.Add(stream);

                stream.StartReceiving()
                    // Open the stream once receiving has started
                    .Then(() => Open(streamIndex))
                    // Starting the receive loop failed
                    .Catch(ex => Close(streamIndex, ex));
            }
        }
    }
}
