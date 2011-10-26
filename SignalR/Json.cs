using System;
using System.Linq;

namespace SignalR
{
    internal static class Json
    {
        internal static string CamelCase(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            return String.Join(".", value.Split('.').Select(n => Char.ToLower(n[0]) + n.Substring(1)));
        }

        internal static string MimeType
        {
            get { return "application/json"; }
        }
    }
}