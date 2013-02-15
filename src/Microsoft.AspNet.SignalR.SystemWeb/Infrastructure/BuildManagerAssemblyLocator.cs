// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Compilation;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.SystemWeb.Infrastructure
{
    public class BuildManagerAssemblyLocator : DefaultAssemblyLocator
    {
        public override IList<Assembly> GetAssemblies()
        {
            return BuildManager.GetReferencedAssemblies().Cast<Assembly>().ToList();
        }
    }
}
