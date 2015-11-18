// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using Windows.ApplicationModel.Resources;

namespace Microsoft.AspNet.SignalR.Client
{
    internal static class ResourcesStore
    {
        private static readonly ResourceLoader ResourceLoader =
            ResourceLoader.GetForViewIndependentUse("Microsoft.AspNet.SignalR.Client.Store/Resources");

        public static string GetResourceString(string resourceName)
        {
            return ResourceLoader.GetString(resourceName);
        }
    }
}
