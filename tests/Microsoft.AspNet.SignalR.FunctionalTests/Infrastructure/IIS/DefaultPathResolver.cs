using System;
using System.IO;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IIS
{
    public class DefaultPathResolver : IPathResolver
    {        
        private readonly string _rootPath;

        public DefaultPathResolver(string rootPath)
        {
            _rootPath = Path.GetFullPath(rootPath);
        }

        public string GetApplicationPath(string applicationName)
        {
            // All sites get the same base path
            return _rootPath;
        }
    }
}
