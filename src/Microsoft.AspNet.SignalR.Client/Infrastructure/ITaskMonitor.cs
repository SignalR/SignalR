// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    internal interface ITaskMonitor
    {
        void TaskStarted();
        void TaskCompleted();
    }
}
