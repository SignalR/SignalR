// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    public static class UrlBuilder
    {
        public static string BuildNegotiate(IConnection connection, string connectionData)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            var urlStringBuilder = CreateBaseUrl("negotiate", connection, null, connectionData);
#if PORTABLE
            AppendNoCacheUrlParam(urlStringBuilder);
#endif
            return Trim(urlStringBuilder);
        }

        public static string BuildStart(IConnection connection, string transport, string connectionData)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (string.IsNullOrWhiteSpace(transport))
            {
                throw new ArgumentNullException("transport");
            }

            return Trim(CreateBaseUrl("start", connection, transport, connectionData));
        }

        public static string BuildConnect(IConnection connection, string transport, string connectionData)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (string.IsNullOrWhiteSpace(transport))
            {
                throw new ArgumentNullException("transport");
            }

            var urlStringBuilder = CreateBaseUrl("connect", connection, transport, connectionData);
            AppendReceiveParameters(urlStringBuilder, connection);

            return Trim(urlStringBuilder);
        }

        public static string BuildReconnect(IConnection connection, string transport, string connectionData)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (string.IsNullOrWhiteSpace(transport))
            {
                throw new ArgumentNullException("transport");
            }

            var urlStringBuilder = CreateBaseUrl("reconnect", connection, transport, connectionData);
            AppendReceiveParameters(urlStringBuilder, connection);

            return Trim(urlStringBuilder);
        }

        public static string BuildPoll(IConnection connection, string transport, string connectionData)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (string.IsNullOrWhiteSpace(transport))
            {
                throw new ArgumentNullException("transport");
            }

            Debug.Assert(connection != null, "connection is null");
            Debug.Assert(!string.IsNullOrWhiteSpace(transport), "invalid transport");

            var urlStringBuilder = CreateBaseUrl("poll", connection, transport, connectionData);
            AppendReceiveParameters(urlStringBuilder, connection);

            return Trim(urlStringBuilder);
        }

        public static string BuildSend(IConnection connection, string transport, string connectionData)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (string.IsNullOrWhiteSpace(transport))
            {
                throw new ArgumentNullException("transport");
            }

            return Trim(CreateBaseUrl("send", connection, transport, connectionData));
        }

        public static string BuildAbort(IConnection connection, string transport, string connectionData)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (string.IsNullOrWhiteSpace(transport))
            {
                throw new ArgumentNullException("transport");
            }

            return Trim(CreateBaseUrl("abort", connection, transport, connectionData));
        }

        private static StringBuilder CreateBaseUrl(string command, IConnection connection, string transport, string connectionData)
        {
            var urlStringBuilder = new StringBuilder();
            urlStringBuilder
                .Append(connection.Url)
                .Append(command)
                .Append("?");

            AppendCommonParameters(urlStringBuilder, connection, transport, connectionData);

            return urlStringBuilder;
        }

        private static void AppendCommonParameters(StringBuilder urlStringBuilder, IConnection connection, string transport, string connectionData)
        {
            AppendClientProtocol(urlStringBuilder, connection);
            AppendTransport(urlStringBuilder, transport);
            AppendConnectionData(urlStringBuilder, connectionData);
            AppendConnectionToken(urlStringBuilder, connection);
            AppendCustomQueryString(urlStringBuilder, connection);
        }

        private static void AppendReceiveParameters(StringBuilder urlStringBuilder, IConnection connection)
        {
            AppendMessageId(urlStringBuilder, connection);
            AppendGroupsToken(urlStringBuilder, connection);
#if PORTABLE
            AppendNoCacheUrlParam(urlStringBuilder);
#endif
        }

        private static string Trim(StringBuilder urlStringBuilder)
        {
            Debug.Assert(urlStringBuilder[urlStringBuilder.Length - 1] == '&', 
                "expected & at the end of the url");

            urlStringBuilder.Length--;
            return urlStringBuilder.ToString();
        }

        private static void AppendClientProtocol(StringBuilder urlStringBuilder, IConnection connection)
        {
            urlStringBuilder
                .Append("clientProtocol=")
                .Append(connection.Protocol)
                .Append("&");
        }

        private static void AppendTransport(StringBuilder urlStringBuilder, string transportName)
        {
            if (transportName != null)
            {
                urlStringBuilder
                    .Append("transport=")
                    .Append(transportName)
                    .Append("&");
            }
        }

        private static void AppendConnectionToken(StringBuilder urlStringBuilder, IConnection connection)
        {
            // connection.ConnectionToken can be set to null in a different thread
            var connectionToken = connection.ConnectionToken;
            if (connectionToken != null)
            {
                urlStringBuilder
                    .Append("connectionToken=")
                    .Append(Uri.EscapeDataString(connectionToken))
                    .Append("&");
            }
        }

        private static void AppendMessageId(StringBuilder urlStringBuilder, IConnection connection)
        {
            // connection.MessageId can be set to null in a different thread
            var messageId = connection.MessageId;
            if (messageId != null)
            {
                urlStringBuilder
                    .Append("messageId=")
                    .Append(Uri.EscapeDataString(messageId))
                    .Append("&");                
            }
        }

        private static void AppendGroupsToken(StringBuilder urlStringBuilder, IConnection connection)
        {
            // connection.GroupsToken can be set to null in a different thread
            var groupsToken = connection.GroupsToken;
            if (groupsToken != null)
            {
                urlStringBuilder
                    .Append("groupsToken=")
                    .Append(Uri.EscapeDataString(groupsToken))
                    .Append("&");
            }
        }

        private static void AppendConnectionData(StringBuilder urlStringBuilder, string connectionData)
        {
            if (!string.IsNullOrEmpty(connectionData))
            {
                urlStringBuilder
                    .Append("connectionData=")
                    .Append(connectionData)
                    .Append("&");
            }
        }

        private static void AppendCustomQueryString(StringBuilder urlStringBuilder, IConnection connection)
        {
            Debug.Assert(
                urlStringBuilder[urlStringBuilder.Length - 1] == '?' ||
                urlStringBuilder[urlStringBuilder.Length - 1] == '&',
                "url should end with a correct separator");

            if (string.IsNullOrEmpty(connection.QueryString))
            {
                return;
            }

            var firstChar = connection.QueryString[0];
            // correct separator is already appended
            if (firstChar == '?' || firstChar == '&')
            {
                urlStringBuilder.Append(connection.QueryString.Substring(1));
            }
            else
            {
                urlStringBuilder.Append(connection.QueryString);
            }

            urlStringBuilder.Append("&");
        }

#if PORTABLE
        private static void AppendNoCacheUrlParam(StringBuilder urlStringBuilder)
        {
            urlStringBuilder
                .Append("noCache=")
                .Append(Guid.NewGuid())
                .Append("&");
        }
#endif

        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "string", Justification = "The string should be a Uri")]
        [SuppressMessage("Microsoft.Usage", "CA2234:PassSystemUriObjectsInsteadOfStrings", Justification = "The input string is being converted to Uri")]
        public static Uri ConvertToWebSocketUri(string uriString)
        {
            var uriBuilder = new UriBuilder(uriString);

            if (uriBuilder.Scheme != "http" && uriBuilder.Scheme != "https")
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture, Resources.Error_InvalidUriScheme, uriBuilder.Scheme));    
            }

            uriBuilder.Scheme = uriBuilder.Scheme == "https" ? "wss" : "ws";

            return uriBuilder.Uri;
        }
    }
}
