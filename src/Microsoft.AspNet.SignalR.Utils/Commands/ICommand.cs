// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace Microsoft.AspNet.SignalR.Utils
{
    internal interface ICommand
    {
        string DisplayName { get; }
        string Help { get; }
        string[] Names { get; }
        int Execute(string[] args);
    }
}
