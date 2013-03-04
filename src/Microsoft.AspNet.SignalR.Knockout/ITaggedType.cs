// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.Knockout
{
    public interface ITaggedType<T>
    {
        string _tag { get; }
        T value { get; }
    }
}
