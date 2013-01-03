using System;
using System.Globalization;

namespace Microsoft.AspNet.SignalR.Hubs
{
    class EmptyJavaScriptProxyGenerator : IJavaScriptProxyGenerator
    {
        public string GenerateProxy(string serviceUrl, bool includeDocComments)
        {
            return String.Format(CultureInfo.InvariantCulture, "throw new Error('{0}');", Resources.Error_JavaScriptProxyDisabled);
        }
    }
}
