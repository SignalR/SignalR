// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.SignalR.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public static class MethodExtensions
    {
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1", Justification = "The condition checks for null parameters")]
        public static bool Matches(this MethodDescriptor methodDescriptor, IList<IJsonValue> parameters)
        {
            if (methodDescriptor == null)
            {
                throw new ArgumentNullException("methodDescriptor");
            }

            if (methodDescriptor.Parameters.Count > 0 && parameters == null)
            {
                return false;
            }                       

            if (methodDescriptor.Parameters.Count != parameters.Count)
            {
                if (methodDescriptor.Parameters.Count < parameters.Count)
                {
                    return false;
                }

                //if params are optional, we can accept the missing parameters
                if (methodDescriptor.Parameters[parameters.Count].IsOptional)
                {
                    return true;
                }
                else 
                {                    
                    //if the last param is Params Array, then we can accept the missing parameter
                    if (methodDescriptor.Parameters[parameters.Count].IsParameterArray && (methodDescriptor.Parameters.Count == parameters.Count + 1))
                    {
                        return true;                        
                    }
                }
                
                //no match
                return false;
            }

            return true;
        }

    }
}
