// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System.Data.Common;

    static class DbConnectionStringBuilderExtentionMethods
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Will be used in the future.")]
        public static bool TryGetStringValue(this DbConnectionStringBuilder builder, string key, out string value)
        {
            object objectValue;

            if (builder.TryGetValue(key, out objectValue))
            {
                value = (string)objectValue;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }
    }
}
