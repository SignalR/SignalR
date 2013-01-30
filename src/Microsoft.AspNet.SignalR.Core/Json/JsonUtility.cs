// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.CodeDom.Compiler;
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
        private static readonly string[] _jsKeywords = new[] { "abstract", "boolean", "break", "byte", "case", "catch", "char", "class", "const", "continue", "debugger", "default", "delete", "do", "double", "else", "enum", "export", "extends", "false", "final", "finally", "float", "for", "function", "goto", "if", "implements", "import", "in", "instanceof", "int", "interface", "long", "native", "new", "null", "package", "private", "protected", "public", "return", "short", "static", "super", "switch", "synchronized", "this", "throw", "throws", "transient", "true", "try", "typeof", "var", "volatile", "void", "while", "with", "NaN", "Infinity", "undefined" };

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
            if (!IsValidJavaScriptCallback(callback))
            {
                throw new InvalidOperationException();
            }
            sb.AppendFormat("{0}(", callback).Append(payload).Append(");");
            return sb.ToString();
        }

        internal static bool IsValidJavaScriptCallback(string callback)
        {
            if (String.IsNullOrWhiteSpace(callback))
            {
                return false;
            }

            var identifiers = callback.Split('.');

            // Check each identifier to ensure it's a valid JS identifier
            foreach (var identifier in identifiers)
            {
                if (!IsValidJavaScriptFunctionName(identifier))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool IsValidJavaScriptFunctionName(string name)
        {
            if (String.IsNullOrWhiteSpace(name) || IsJavaScriptReservedWord(name))
            {
                return false;
            }

            // JavaScript identifier must start with a letter or a '$' or an '_' char
            var firstChar = name[0];
            if (!IsValidJavaScriptIdentifierStartChar(firstChar))
            {
                return false;
            }

            for (var i = 1; i < name.Length; i++)
            {
                // Characters can be a letter, digit, '$' or '_'
                if (!IsValidJavaScriptIdenfitierNonStartChar(name[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidJavaScriptIdentifierStartChar(char startChar)
        {
            return Char.IsLetter(startChar) || startChar == '$' || startChar == '_';
        }

        private static bool IsValidJavaScriptIdenfitierNonStartChar(char identifierChar)
        {
            return Char.IsLetterOrDigit(identifierChar) || identifierChar == '$' || identifierChar == '_';
        }

        private static bool IsJavaScriptReservedWord(string word)
        {
            return _jsKeywords.Contains(word);
        }
    }
}
