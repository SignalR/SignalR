// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.Hubs
{
    /// <summary>
    /// 
    /// </summary>
    public interface IHubPipeline
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pipelineModule"></param>
        /// <returns></returns>
        IHubPipeline AddModule(IHubPipelineModule pipelineModule);
    }
}
