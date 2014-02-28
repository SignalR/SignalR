// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class ExceptionContext
    {
        private object _result;

        public ExceptionContext(Exception error)
        {
            Error = error;
        }

        /// <summary>
        /// The exception to be sent to the calling client.
        /// This will be overridden by a generic Exception unless Error is a <see cref="HubException"/>
        /// or <see cref="HubConfiguration.EnableDetailedErrors"/> is set to true.
        /// </summary>
        public Exception Error { get; set; }

        /// <summary>
        /// The value to return in lieu of throwing Error. Whenever Result is set, Error will be set to null.
        /// </summary>
        public object Result
        {
            get
            {
                return _result;
            }
            set
            {
                Error = null;
                _result = value;
            }
        }
    }
}
