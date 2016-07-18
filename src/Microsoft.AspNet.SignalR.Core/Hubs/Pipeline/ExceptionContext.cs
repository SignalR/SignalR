// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
