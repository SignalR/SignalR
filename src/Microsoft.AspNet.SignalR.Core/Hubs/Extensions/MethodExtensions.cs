// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public static class MethodExtensions
    {
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1", Justification = "The condition checks for null parameters")]
        public static bool Matches(this MethodDescriptor methodDescriptor, params IJsonValue[] parameters)
        {
            if (methodDescriptor == null)
            {
                throw new ArgumentNullException("methodDescriptor");
            }

            if ((methodDescriptor.Parameters.Count > 0 && parameters == null)
                || methodDescriptor.Parameters.Count != parameters.Length)
            {
                return false;
            }

            return true;
        }
    }
}
