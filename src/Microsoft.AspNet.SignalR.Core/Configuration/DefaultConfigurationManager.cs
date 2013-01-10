// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Configuration
{
    public class DefaultConfigurationManager : IConfigurationManager
    {
        // The below effectively sets the minimum heartbeat to once per second.
        private static readonly TimeSpan _minimumKeepAlive = TimeSpan.FromSeconds(2);
        private const int _minimumKeepAlivesPerDisconnectTimeout = 3;

        // if _minimumDisconnectTimeout != 6 seconds, update the ArguementOutOfRanceExceptionMessage below
        private static readonly TimeSpan _minimumDisconnectTimeout = TimeSpan.FromTicks(_minimumKeepAlive.Ticks * _minimumKeepAlivesPerDisconnectTimeout);

        private TimeSpan _keepAlive;
        private TimeSpan _disconnectTimeout;

        public DefaultConfigurationManager()
        {
            ConnectionTimeout = TimeSpan.FromSeconds(110);
            DisconnectTimeout = TimeSpan.FromSeconds(40);
            DefaultMessageBufferSize = 1000;
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
                if (value != TimeSpan.Zero && value < _minimumDisconnectTimeout)
                {
                    throw new ArgumentOutOfRangeException(Resources.Error_NonZeroDisconnectTimeoutMustBeAtLeastSixSeconds);
                }

                _disconnectTimeout = value;

                // TODO: How do we ensure developers don't configure custom KeepAlive values before setting DisconnectTimeout?
                // TODO: Should setting the DisconnectTimeout to zero disable KeepAlives?
                _keepAlive = TimeSpan.FromTicks(_disconnectTimeout.Ticks / _minimumKeepAlivesPerDisconnectTimeout);
            }
        }
        
        public TimeSpan KeepAlive
        {
            get
            {
                return _keepAlive;
            }
            set
            {
                if (value != TimeSpan.Zero && value < _minimumKeepAlive)
                {
                    throw new ArgumentOutOfRangeException(Resources.Error_KeepAliveMustBeGreaterThanTwoSeconds);
                }

                // TODO: 
                if (value > TimeSpan.FromTicks(_disconnectTimeout.Ticks / _minimumKeepAlivesPerDisconnectTimeout))
                {
                    throw new ArgumentOutOfRangeException(Resources.Error_KeepAliveMustBeNoMoreThanAThirdOfTheDisconnectTimeout);
                }

                _keepAlive = value;
            }
        }

        public int DefaultMessageBufferSize
        {
            get;
            set;
        }
    }
}
