using System;
using System.Linq;
using System.Text;

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

        public static string JsonpMimeType
        {
            get { return "text/javascript"; }
        }

        public static string CreateJsonpCallback(string callback, string payload)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}(", callback).Append(payload).Append(");");
            return sb.ToString();
        }
    }
}