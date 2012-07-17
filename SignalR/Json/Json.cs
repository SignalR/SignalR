using System;
using System.Linq;
using System.Text;

namespace SignalR
{
    /// <summary>
    /// Helper class for common JSON operations.
    /// </summary>
    public static class Json
    {
        /// <summary>
        /// Converts the specified name to camel case.
        /// </summary>
        /// <param name="name">The name to convert.</param>
        /// <returns>A camel cased version of the specified name.</returns>
        public static string CamelCase(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("value");
            }
            return String.Join(".", name.Split('.').Select(n => Char.ToLower(n[0]) + n.Substring(1)));
        }

        /// <summary>
        /// Gets a string that returns JSON mime type "application/json".
        /// </summary>
        public static string MimeType
        {
            get { return "application/json"; }
        }

        /// <summary>
        /// Gets a string that returns JSONP mime type "text/javascript".
        /// </summary>
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