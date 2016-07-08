// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// Describes a parameter resolver for resolving parameter-matching values based on provided information.
    /// </summary>
    public interface IParameterResolver
    {
        /// <summary>
        /// Resolves method parameter values based on provided objects.
        /// </summary>
        /// <param name="method">Method descriptor.</param>
        /// <param name="values">List of values to resolve parameter values from.</param>
        /// <returns>Array of parameter values.</returns>
        IList<object> ResolveMethodParameters(MethodDescriptor method, IList<IJsonValue> values);
    }
}
