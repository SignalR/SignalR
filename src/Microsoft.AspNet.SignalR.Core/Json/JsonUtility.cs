﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Owin;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SignalR.Json
{
    /// <summary>
    /// Helper class for common JSON operations.
    /// </summary>
    public static class JsonUtility
    {
        private const int DefaultMaxDepth = 20;

        // JavaScript keywords taken from http://www.ecma-international.org/publications/files/ECMA-ST/Ecma-262.pdf
        //   Sections: 7.6.1.1, 7.6.1.2
        // Plus the implicity globals "NaN", "undefined", "Infinity"
        private static readonly string[] _jsKeywords = new[] { "break", "do", "instanceof", "typeof", "case", "else", "new", "var", "catch", "finally", "return", "void", "continue", "for", "switch", "while", "debugger", "function", "this", "with", "default", "if", "throw", "delete", "in", "try", "class", "enum", "extends", "super", "const", "export", "import", "implements", "let", "private", "public", "yield", "interface", "package", "protected", "static", "NaN", "undefined", "Infinity" };

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
        /// Gets a string that returns JSON mime type "application/json; charset=UTF-8".
        /// </summary>
        public static string JsonMimeType
        {
            get { return "application/json; charset=UTF-8"; }
        }

        /// <summary>
        /// Gets a string that returns JSONP mime type "application/javascript; charset=UTF-8".
        /// </summary>
        public static string JavaScriptMimeType
        {
            get { return "application/javascript; charset=UTF-8"; }
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

        /// <summary>
        /// Creates a default <see cref="T:Newtonsoft.Json.JsonSerializerSettings"/> instance.
        /// </summary>
        /// <returns>The newly created <see cref="T:Newtonsoft.Json.JsonSerializerSettings"/>.</returns>
        public static JsonSerializerSettings CreateDefaultSerializerSettings()
        {
            return new JsonSerializerSettings() { MaxDepth = DefaultMaxDepth };
        }

        /// <summary>
        /// Creates a <see cref="T:Newtonsoft.Json.JsonSerializer"/> instance with the default setting. 
        /// </summary>
        /// <returns>The newly created <see cref="T:Newtonsoft.Json.JsonSerializerSettings"/>.</returns>
        public static JsonSerializer CreateDefaultSerializer()
        {
            return JsonSerializer.Create(CreateDefaultSerializerSettings());
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

        internal static bool TryRejectJSONPRequest(ConnectionConfiguration config,
                                                   IOwinContext context)
        {
            // If JSONP is enabled then do nothing
            if (config.EnableJSONP)
            {
                return false;
            }

            string callback = context.Request.Query.Get("callback");

            // The request isn't a JSONP request so do nothing
            if (String.IsNullOrEmpty(callback))
            {
                return false;
            }

            // Disable the JSONP request with a 403
            context.Response.StatusCode = 403;
            context.Response.ReasonPhrase = Resources.Forbidden_JSONPDisabled;
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
