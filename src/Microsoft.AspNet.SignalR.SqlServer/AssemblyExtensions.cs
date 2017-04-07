// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Reflection;

namespace Microsoft.AspNet.SignalR
{
    internal static class AssemblyExtensions
    {
        /// <summary>
        /// Loads an embedded string resource from the assembly.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded resource.</param>
        /// <param name="name">The resource name.</param>
        /// <returns>The embedded resource string.</returns>
        public static string StringResource(this Assembly assembly, string name)
        {
            string resource;
            using (var resourceStream = assembly.GetManifestResourceStream(name))
            {
                var reader = new StreamReader(resourceStream);
                resource = reader.ReadToEnd();
            }
            return resource;
        }
    }
}
