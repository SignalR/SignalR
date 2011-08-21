using System.Web.Compilation;

namespace SignalR.Infrastructure {
    internal class NullJavaScriptMinifier : IJavaScriptMinifier {
        public string Minify(string source) { 
            return source;
        }
    }
}
