// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// Holds information about a single hub method parameter.
    /// </summary>
    public class ParameterDescriptor
    {
        /// <summary>
        /// Parameter name.
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// Parameter type.
        /// </summary>
        public virtual Type ParameterType { get; set; }

        /// <summary>
        /// Parameter IsOptional.
        /// </summary>
        public virtual bool IsOptional { get; set; }

        /// <summary>
        /// Parameter DefaultValue.
        /// </summary>
        public virtual object DefaultValue { get; set; }
        /// <summary>
        /// Parameter IsParameterArray.
        /// </summary>
        public virtual bool IsParameterArray { get; set; }
    }
}

