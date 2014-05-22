// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    internal class UrlBuilder
    {
        private readonly StringBuilder _urlStringBuilder = new StringBuilder();

        internal string BuildNegotiate(IConnection connection, string connectionData)
        {
            Debug.Assert(connection != null, "connection is null");

            CreateBaseUrl("negotiate", connection, null, connectionData);
#if PORTABLE
            AppendNoCacheUrlParam();
#endif
            return Trim();
        }

        public string BuildStart(IConnection connection, string transport, string connectionData)
        {
            Debug.Assert(connection != null, "connection is null");
            Debug.Assert(!string.IsNullOrWhiteSpace(transport), "invalid transport");

            CreateBaseUrl("start", connection, transport, connectionData);

            return Trim();
        }

        public string BuildConnect(IConnection connection, string transport, string connectionData)
        {
            Debug.Assert(connection != null, "connection is null");
            Debug.Assert(!string.IsNullOrWhiteSpace(transport), "invalid transport");

            CreateBaseUrl("connect", connection, transport, connectionData);
            AppendReceiveParameters(connection);

            return Trim();
        }

        public string BuildReconnect(IConnection connection, string transport, string connectionData)
        {
            Debug.Assert(connection != null, "connection is null");
            Debug.Assert(!string.IsNullOrWhiteSpace(transport), "invalid transport");

            CreateBaseUrl("reconnect", connection, transport, connectionData);
            AppendReceiveParameters(connection);

            return Trim();
        }

        public string BuildPoll(IConnection connection, string transport, string connectionData)
        {
            Debug.Assert(connection != null, "connection is null");
            Debug.Assert(!string.IsNullOrWhiteSpace(transport), "invalid transport");

            CreateBaseUrl("poll", connection, transport, connectionData);
            AppendReceiveParameters(connection);

            return Trim();
        }

        public string BuildSend(IConnection connection, string transport, string connectionData)
        {
            Debug.Assert(connection != null, "connection is null");
            Debug.Assert(!string.IsNullOrWhiteSpace(transport), "invalid transport");

            CreateBaseUrl("send", connection, transport, connectionData);

            return Trim();
        }

        public string BuildAbort(IConnection connection, string transport, string connectionData)
        {
            Debug.Assert(connection != null, "connection is null");
            Debug.Assert(!string.IsNullOrWhiteSpace(transport), "invalid transport");

            CreateBaseUrl("abort", connection, transport, connectionData);
            return Trim();
        }

        private void CreateBaseUrl(string command, IConnection connection, string transport, string connectionData)
        {
            _urlStringBuilder.Length = 0;

            _urlStringBuilder
                .Append(connection.Url)
                .Append(command)
                .Append("?");

            AppendCommonParameters(connection, transport, connectionData);
        }

        private void AppendCommonParameters(IConnection connection, string transport, string connectionData)
        {
            AppendClientProtocol(connection);
            AppendTransport(transport);
            AppendConnectionData(connectionData);
            AppendConnectionToken(connection);
            AppendCustomQueryString(connection);
        }

        private void AppendReceiveParameters(IConnection connection)
        {
            AppendMessageId(connection);
            AppendGroupsToken(connection);
#if PORTABLE
            AppendNoCacheUrlParam();
#endif
        }

        private string Trim()
        {
            Debug.Assert(_urlStringBuilder[_urlStringBuilder.Length - 1] == '&', 
                "expected & at the end of the url");

            _urlStringBuilder.Length--;
            return _urlStringBuilder.ToString();
        }

        private void AppendClientProtocol(IConnection connection)
        {
            _urlStringBuilder
                .Append("clientProtocol=")
                .Append(connection.Protocol)
                .Append("&");
        }

        private void AppendTransport(string transportName)
        {
            if (transportName != null)
            {
                _urlStringBuilder
                    .Append("transport=")
                    .Append(transportName)
                    .Append("&");
            }
        }

        private void AppendConnectionToken(IConnection connection)
        {
            if (connection.ConnectionToken != null)
            {
                _urlStringBuilder
                    .Append("connectionToken=")
                    .Append(Uri.EscapeDataString(connection.ConnectionToken))
                    .Append("&");
            }
        }

        private void AppendMessageId(IConnection connection)
        {
            if (connection.MessageId != null)
            {
                _urlStringBuilder
                    .Append("messageId=")
                    .Append(Uri.EscapeDataString(connection.MessageId))
                    .Append("&");                
            }
        }

        private void AppendGroupsToken(IConnection connection)
        {
            if (connection.GroupsToken != null)
            {
                _urlStringBuilder
                    .Append("groupsToken=")
                    .Append(Uri.EscapeDataString(connection.GroupsToken))
                    .Append("&");
            }
        }

        private void AppendConnectionData(string connectionData)
        {
            if (!string.IsNullOrEmpty(connectionData))
            {
                _urlStringBuilder
                    .Append("connectionData=")
                    .Append(connectionData)
                    .Append("&");
            }
        }

        private void AppendCustomQueryString(IConnection connection)
        {
            Debug.Assert(
                _urlStringBuilder[_urlStringBuilder.Length - 1] == '?' ||
                _urlStringBuilder[_urlStringBuilder.Length - 1] == '&',
                "url should ends with a correct separator");

            if (string.IsNullOrEmpty(connection.QueryString))
            {
                return;
            }

            var firstChar = connection.QueryString[0];
            // correct separator is already appended
            if (firstChar == '?' || firstChar == '&')
            {
                _urlStringBuilder.Append(connection.QueryString.Substring(1));
            }
            else
            {
                _urlStringBuilder.Append(connection.QueryString);
            }

            _urlStringBuilder.Append("&");
        }

#if PORTABLE
        private void AppendNoCacheUrlParam()
        {
            _urlStringBuilder
                .Append("noCache=")
                .Append(Guid.NewGuid())
                .Append("&");
        }
#endif
    }
}
