// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Json
{
    /// <summary>
    /// Represents a JSON value.
    /// </summary>
    public interface IJsonValue
    {
        /// <summary>
        /// Converts the parameter value to the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to convert the parameter to.</param>
        /// <returns>The converted parameter value.</returns>
        object ConvertTo(Type type);

        /// <summary>
        /// Determines if the parameter can be converted to the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to check.</param>
        /// <returns>True if the parameter can be converted to the specified <see cref="Type"/>, false otherwise.</returns>
        bool CanConvertTo(Type type);
    }
}
