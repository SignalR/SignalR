using System.Text;

namespace Microsoft.AspNet.SignalR.Client.LibExtensions
{
    static class StringBuilderExt
    {
        public static void Clear(this StringBuilder sb)
        {
            sb.Length = 0;
        }
    }
}
