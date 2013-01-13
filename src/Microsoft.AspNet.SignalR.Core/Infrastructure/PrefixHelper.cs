
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.SignalR.Infrastructure
{
    internal static class PrefixHelper
    {
        // Hubs
        internal const string HubPrefix = "h-";
        internal const string HubGroupPrefix = "hg-";
        internal const string HubConnectionIdPrefix = "hc-";

        // Persistent Connections
        internal const string PersistentConnectionPrefix = "pc-";
        internal const string PersistentConnectionGroupPrefix = "pcg-";

        // Both
        internal const string ConnectionIdPrefix = "c-";
        internal const string AckPrefix = "ack-";


        internal static string GetConnectionId(string connectionId)
        {
            return ConnectionIdPrefix + connectionId;
        }

        internal static string GetHubConnectionId(string connectionId)
        {
            return HubConnectionIdPrefix + connectionId;
        }

        internal static string GetHubName(string connectionId)
        {
            return HubPrefix + connectionId;
        }

        internal static string GetHubGroupName(string groupName)
        {
            return HubGroupPrefix + groupName;
        }

        internal static string GetPersistentConnectionGroupName(string groupName)
        {
            return PersistentConnectionGroupPrefix + groupName;
        }

        internal static string GetPersistentConnectionName(string connectionName)
        {
            return PersistentConnectionPrefix + connectionName;
        }

        internal static string GetAck(string connectionId)
        {
            return AckPrefix + connectionId;
        }

        internal static IList<string> GetPrefixedConnectionIds(IList<string> connectionIds)
        {
            if (connectionIds.Count == 0)
            {
                return ListHelper<string>.Empty;
            }

            return connectionIds.Select(PrefixHelper.GetConnectionId).ToList();
        }

        internal static IEnumerable<string> RemoveGroupPrefixes(IEnumerable<string> groups)
        {
            return groups.Select(PrefixHelper.RemoveGroupPrefix);
        }

        internal static string RemoveGroupPrefix(string name)
        {
            if (name.StartsWith(HubGroupPrefix, StringComparison.Ordinal))
            {
                return name.Substring(HubGroupPrefix.Length);
            }

            if (name.StartsWith(PersistentConnectionGroupPrefix, StringComparison.Ordinal))
            {
                return name.Substring(PersistentConnectionGroupPrefix.Length);
            }

            return name;
        }
    }
}
