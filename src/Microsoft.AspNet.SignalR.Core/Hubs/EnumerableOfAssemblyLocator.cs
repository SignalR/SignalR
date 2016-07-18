// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class EnumerableOfAssemblyLocator : IAssemblyLocator
    {
        private readonly IEnumerable<Assembly> _assemblies;

        public EnumerableOfAssemblyLocator(IEnumerable<Assembly> assemblies)
        {
            _assemblies = assemblies;
        }

        public IList<Assembly> GetAssemblies()
        {
            return _assemblies.ToList();
        }
    }
}
