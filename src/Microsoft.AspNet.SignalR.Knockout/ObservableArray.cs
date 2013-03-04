// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.Knockout
{
    public class ObservableArray<T> : ITaggedType<T[]>
    {
        public ObservableArray()
        {
        }

        public ObservableArray(T[] value)
        {
            this.value = value;
        }

        public string _tag
        {
            get { return "koObservable"; }
        }

        public T[] value { get; set; }
    }
}
