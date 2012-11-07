// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR
{
    public class Connection : IConnection, ITransportConnection, ISubscriber
    {
        private readonly IMessageBus _bus;
        private readonly IJsonSerializer _serializer;
        private readonly string _baseSignal;
        private readonly string _connectionId;
        private readonly HashSet<string> _signals;
        private readonly DiffSet<string> _groups;
        private ICollection<string> _lastGroups;
        private readonly IPerformanceCounterManager _counters;

        private bool _disconnected;
        private bool _aborted;
        private readonly Lazy<TraceSource> _traceSource;
        private readonly IAckHandler _ackHandler;

        public Connection(IMessageBus newMessageBus,
                          IJsonSerializer jsonSerializer,
                          string baseSignal,
                          string connectionId,
                          IEnumerable<string> signals,
                          IEnumerable<string> groups,
                          ITraceManager traceManager,
                          IAckHandler ackHandler,
                          IPerformanceCounterManager performanceCounterManager)
        {
            _bus = newMessageBus;
            _serializer = jsonSerializer;
            _baseSignal = baseSignal;
            _connectionId = connectionId;
            _signals = new HashSet<string>(signals, StringComparer.OrdinalIgnoreCase);
            _groups = new DiffSet<string>(groups);
            _lastGroups = new Collection<string>();
            _traceSource = new Lazy<TraceSource>(() => traceManager["SignalR.Connection"]);
            _ackHandler = ackHandler;
            _counters = performanceCounterManager;
        }

        public string DefaultSignal
        {
            get
            {
                return _baseSignal;
            }
        }

        IEnumerable<string> ISubscriber.EventKeys
        {
            get
            {
                return Signals;
            }
        }

        public event Action<string> EventAdded;

        public event Action<string> EventRemoved;

        public string Identity
        {
            get
            {
                return _connectionId;
            }
        }

        private IEnumerable<string> Signals
        {
            get
            {
                return _signals.Concat(_groups.GetSnapshot());
            }
        }

        private TraceSource Trace
        {
            get
            {
                return _traceSource.Value;
            }
        }

        public Task Send(ConnectionMessage message)
        {
            Message busMessage = CreateMessage(message.Signal, message.Value);

            if (message.ExcludedSignals != null)
            {
                busMessage.Filter = String.Join("|", message.ExcludedSignals);
            }

            if (busMessage.WaitForAck)
            {
                Task ackTask = _ackHandler.CreateAck(busMessage.CommandId);
                return _bus.Publish(busMessage).Then(task => task, ackTask);
            }

            return _bus.Publish(busMessage);
        }

        private Message CreateMessage(string key, object value)
        {
            var command = value as Command;
            var message = new Message(_connectionId, key, _serializer.Stringify(value));

            if (command != null)
            {
                // Set the command id
                message.CommandId = command.Id;
                message.WaitForAck = command.WaitForAck;
            }

            return message;
        }

        public Task<PersistentResponse> ReceiveAsync(string messageId, CancellationToken cancel, int maxMessages)
        {
            return _bus.ReceiveAsync<PersistentResponse>(this, messageId, cancel, maxMessages, GetResponse, (result, response) =>
            {
                response.MessageId = result.LastMessageId;
            });
        }
        public IDisposable Receive(string messageId, Func<PersistentResponse, Task<bool>> callback, int maxMessages)
        {
            return _bus.Subscribe(this, messageId, result =>
            {
                Task<bool> keepGoing = callback(GetResponse(result));
                if (result.Terminal)
                {
                    keepGoing = TaskAsyncHelper.False;
                }
                return keepGoing;
            },
            maxMessages);
        }

        private PersistentResponse GetResponse(MessageResult result)
        {
            // Do a single sweep through the results to process commands and extract values
            ProcessResults(result);

            var response = new PersistentResponse(ExcludeMessage)
            {
                MessageId = result.LastMessageId,
                Messages = result.Messages,
                Disconnect = _disconnected,
                Aborted = _aborted,
                TotalCount = result.TotalCount,
            };

            PopulateResponseState(response);

            _counters.ConnectionMessagesReceivedTotal.IncrementBy(result.TotalCount);
            _counters.ConnectionMessagesReceivedPerSec.IncrementBy(result.TotalCount);

            return response;
        }

        private bool ExcludeMessage(Message message)
        {
            if (String.IsNullOrEmpty(message.Filter))
            {
                return false;
            }

            string[] exclude = message.Filter.Split('|');

            return exclude.Any(signal => Identity.Equals(signal, StringComparison.OrdinalIgnoreCase) ||
                                                    _signals.Contains(signal) ||
                                                    _groups.Contains(signal));
        }

        private void ProcessResults(MessageResult result)
        {
            result.Messages.Enumerate(message => message.IsAck || message.IsCommand,
                                      message =>
                                      {
                                          if (message.IsAck)
                                          {
                                              _ackHandler.TriggerAck(message.CommandId);
                                          }
                                          else if (message.IsCommand)
                                          {
                                              var command = _serializer.Parse<Command>(message.Value);
                                              ProcessCommand(command);

                                              // Only send the ack if this command is waiting for it
                                              if (message.WaitForAck)
                                              {
                                                  // If we're on the same box and there's a pending ack for this command then
                                                  // just trip it
                                                  if (!_ackHandler.TriggerAck(message.CommandId))
                                                  {
                                                      _bus.Ack(_connectionId, message.Key, message.CommandId).Catch();
                                                  }
                                              }
                                          }
                                      });
        }

        private void ProcessCommand(Command command)
        {
            switch (command.CommandType)
            {
                case CommandType.AddToGroup:
                    {
                        var name = command.Value;

                        if (EventAdded != null)
                        {
                            _groups.Add(name);
                            EventAdded(name);
                        }
                    }
                    break;
                case CommandType.RemoveFromGroup:
                    {
                        var name = command.Value;

                        if (EventRemoved != null)
                        {
                            _groups.Remove(name);
                            EventRemoved(name);
                        }
                    }
                    break;
                case CommandType.Disconnect:
                    _disconnected = true;
                    break;
                case CommandType.Abort:
                    _aborted = true;
                    break;
            }
        }

        private void PopulateResponseState(PersistentResponse response)
        {
            var groupDiff = _groups.GetDiff();

            // Set the groups on the outgoing transport data
            if (groupDiff.Added.Count > 0)
            {
                response.AddedGroups = groupDiff.Added;
            }
            if (groupDiff.Removed.Count > 0)
            {
                response.RemovedGroups = groupDiff.Removed;
            }
        }
    }
}
