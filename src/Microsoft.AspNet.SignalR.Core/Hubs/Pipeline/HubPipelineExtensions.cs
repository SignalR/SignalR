// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public static class HubPipelineExtensions
    {
        public static void EnableAutoRejoiningGroups(this IHubPipeline pipeline)
        {
            if (pipeline == null)
            {
                throw new ArgumentNullException("pipeline");
            }

            pipeline.AddModule(new AutoRejoiningGroupsModule());
        }

        public static void RequireAuthentication(this IHubPipeline pipeline)
        {
            if (pipeline == null)
            {
                throw new ArgumentNullException("pipeline");
            }

            var authorizer = new AuthorizeAttribute();
            pipeline.AddModule(new AuthorizeModule(globalConnectionAuthorizer: authorizer, globalInvocationAuthorizer: authorizer));
        }
    }
}
