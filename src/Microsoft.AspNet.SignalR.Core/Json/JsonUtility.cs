// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.AspNet.SignalR.Json
{
    /// <summary>
    /// Helper class for common JSON operations.
    /// </summary>
    public static class JsonUtility
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
                throw new ArgumentNullException("name");
            }

            return String.Join(".", name.Split('.').Select(n => Char.ToLower(n[0], CultureInfo.InvariantCulture) + n.Substring(1)));
        }

        /// <summary>
        /// Gets a string that returns JSON mime type "application/json".
        /// </summary>
        public static string MimeType
        {
            get { return "application/json; charset=UTF-8"; }
        }

        /// <summary>
        /// Gets a string that returns JSONP mime type "text/javascript".
        /// </summary>
        public static string JsonpMimeType
        {
            get { return "text/javascript; charset=UTF-8"; }
        }

        public static string CreateJsonpCallback(string callback, string payload)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}(", callback).Append(payload).Append(");");
            return sb.ToString();
        }
    }
}
