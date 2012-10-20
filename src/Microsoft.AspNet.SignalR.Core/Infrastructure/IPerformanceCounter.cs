// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.


namespace Microsoft.AspNet.SignalR.Infrastructure
{
    public interface IPerformanceCounter
    {
        long Decrement();
        long Increment();
        long IncrementBy(long value);
        void NextSample();
        long RawValue { get; set; }
        void Close();
        void RemoveInstance();
    }
}
