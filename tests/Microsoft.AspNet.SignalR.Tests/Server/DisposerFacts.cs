// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class DisposerFacts
    {
        [Fact]
        public void DisposeDisposes()
        {
            bool disposed = false;
            var disposable = new DisposableAction(() => { disposed = true; });

            var disposer = new Disposer();
            disposer.Set(disposable);

            Assert.False(disposed);

            disposer.Dispose();

            Assert.True(disposed);
        }

        [Fact]
        public void SetDisposesIfShouldDispose()
        {
            bool disposed = false;
            var disposable = new DisposableAction(() => { disposed = true; });
            var disposer = new Disposer();

            Assert.False(disposed);
            disposer.Dispose();
            
            disposer.Set(disposable);
            Assert.True(disposed);
        }

        [Fact]
        public void MultipleDisposes()
        {
            bool disposed = false;
            var disposable = new DisposableAction(() => { disposed = true; });
            var disposer = new Disposer();

            Assert.False(disposed);
            disposer.Dispose();
            disposer.Dispose();
            disposer.Dispose();
            disposer.Dispose();

            disposer.Set(disposable);
            Assert.True(disposed);
        }
    }
}
