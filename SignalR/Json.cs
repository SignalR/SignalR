using System;
using System.Linq;

namespace SignalR
{
    public static class Json
    {
        public static string CamelCase(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            return String.Join(".", value.Split('.').Select(n => Char.ToLower(n[0]) + n.Substring(1)));
        }

        public static string MimeType
        {
            get { return "application/json"; }
        }
    }
}