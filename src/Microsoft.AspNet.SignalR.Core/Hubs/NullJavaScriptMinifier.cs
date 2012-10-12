using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class NullJavaScriptMinifier : IJavaScriptMinifier
    {
        public static readonly NullJavaScriptMinifier Instance = new NullJavaScriptMinifier();

        public string Minify(string source)
        {
            return source;
        }
    }
}
