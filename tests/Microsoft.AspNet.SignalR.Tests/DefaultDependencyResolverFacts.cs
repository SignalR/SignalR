using System;
using Microsoft.AspNet.SignalR.Hubs;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class DefaultDependencyResolverFacts
    {
        [Fact]
        public void DisposablesAreTrackedAndDisposed()
        {
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(MyDisposable), () => new MyDisposable());

            var disposable = resolver.Resolve<MyDisposable>();
            resolver.Dispose();
            Assert.True(disposable.Disposed);
        }

        [Fact]
        public void UntrackedDisposablesAreNotTracked()
        {
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(MyUntrackedDisposable), () => new MyUntrackedDisposable());

            var untrackedDisposable = resolver.Resolve<MyUntrackedDisposable>();
            var untrackedDisposableWeakRef = new WeakReference<MyUntrackedDisposable>(untrackedDisposable);
            
            untrackedDisposable = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.False(untrackedDisposableWeakRef.TryGetTarget(out untrackedDisposable));
        }

        /// <summary>
        /// Test fix for Bug-3208.  The DefaultDependencyResolver should not retain Hub instances that lead to leaks.
        /// </summary>
        [Fact]
        public void HubReferencesAreNotRetained()
        {
            var resolver = new DefaultDependencyResolver();
            resolver.Register(typeof(DontLeakMeHub), () => new DontLeakMeHub());

            var hub = resolver.Resolve<DontLeakMeHub>();
            Assert.NotNull(hub);

            var hubWeakRef = new WeakReference<DontLeakMeHub>(hub);
            hub.Dispose();
            hub = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.False(hubWeakRef.TryGetTarget(out hub));
        }

        private class MyDisposable : IDisposable
        {
            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        private class MyUntrackedDisposable : MyDisposable, IUntrackedDisposable
        {
        }

        private class DontLeakMeHub : Hub
        {
        }
    }
}
