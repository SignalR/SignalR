// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Configuration
{
    public class DefaultConfigurationManager : IConfigurationManager
    {
        // The below effectively sets the minimum heartbeat to once per second.
        // if _minimumKeepAlive != 2 seconds, update the ArguementOutOfRanceExceptionMessage below
        private static readonly TimeSpan _minimumKeepAlive = TimeSpan.FromSeconds(2);

        // if _minimumKeepAlivesPerDisconnectTimeout != 3, update the ArguementOutOfRanceExceptionMessage below
        private const int _minimumKeepAlivesPerDisconnectTimeout = 3;

        internal const int DefaultMaxScaleoutMappingsPerStream = 65535;

        // if _minimumDisconnectTimeout != 6 seconds, update the ArguementOutOfRanceExceptionMessage below
        private static readonly TimeSpan _minimumDisconnectTimeout = TimeSpan.FromTicks(_minimumKeepAlive.Ticks * _minimumKeepAlivesPerDisconnectTimeout);

        private bool _keepAliveConfigured;
        private TimeSpan? _keepAlive;
        private TimeSpan _disconnectTimeout;
        private int _maxScaleoutMappingPerStream;

        public DefaultConfigurationManager()
        {
            ConnectionTimeout = TimeSpan.FromSeconds(110);
            DisconnectTimeout = TimeSpan.FromSeconds(30);
            DefaultMessageBufferSize = 1000;
            MaxIncomingWebSocketMessageSize = 64 * 1024; // 64 KB
            TransportConnectTimeout = TimeSpan.FromSeconds(5);
            LongPollDelay = TimeSpan.Zero;
            MaxScaleoutMappingsPerStream = DefaultMaxScaleoutMappingsPerStream;
        }

        // TODO: Should we guard against negative TimeSpans here like everywhere else?
        public TimeSpan ConnectionTimeout
        {
            get;
            set;
        }

        public TimeSpan DisconnectTimeout
        {
            get
            {
                return _disconnectTimeout;
            }
            set
            {
                if (value < _minimumDisconnectTimeout)
                {
                    throw new ArgumentOutOfRangeException("value", Resources.Error_DisconnectTimeoutMustBeAtLeastSixSeconds);
                }

                if (_keepAliveConfigured)
                {
                    throw new InvalidOperationException(Resources.Error_DisconnectTimeoutCannotBeConfiguredAfterKeepAlive);
                }

                _disconnectTimeout = value;
                _keepAlive = TimeSpan.FromTicks(_disconnectTimeout.Ticks / _minimumKeepAlivesPerDisconnectTimeout);
            }
        }

        public TimeSpan? KeepAlive
        {
            get
            {
                return _keepAlive;
            }
            set
            {
                if (value < _minimumKeepAlive)
                {
                    throw new ArgumentOutOfRangeException("value", Resources.Error_KeepAliveMustBeGreaterThanTwoSeconds);
                }

                if (value > TimeSpan.FromTicks(_disconnectTimeout.Ticks / _minimumKeepAlivesPerDisconnectTimeout))
                {
                    throw new ArgumentOutOfRangeException("value", Resources.Error_KeepAliveMustBeNoMoreThanAThirdOfTheDisconnectTimeout);
                }

                _keepAlive = value;
                _keepAliveConfigured = true;
            }
        }

        public int DefaultMessageBufferSize
        {
            get;
            set;
        }

        public int? MaxIncomingWebSocketMessageSize
        {
            get;
            set;
        }

        public TimeSpan TransportConnectTimeout
        {
            get;
            set;
        }

        public TimeSpan LongPollDelay
        {
            get;
            set;
        }

        public int MaxScaleoutMappingsPerStream
        {
            get
            {
                return _maxScaleoutMappingPerStream;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), Resources.Error_MaxScaleoutMappingsPerStreamMustBeNonNegative);
                }

                _maxScaleoutMappingPerStream = value;
            }
        }
    }
}
