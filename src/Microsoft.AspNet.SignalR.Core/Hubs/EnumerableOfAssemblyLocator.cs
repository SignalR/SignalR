// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

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
