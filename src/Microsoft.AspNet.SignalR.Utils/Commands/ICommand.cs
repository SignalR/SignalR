// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.


namespace Microsoft.AspNet.SignalR.Utils
{
    internal interface ICommand
    {
        string DisplayName { get; }
        string Help { get; }
        string[] Names { get; }
        void Execute(string[] args);
    }
}
