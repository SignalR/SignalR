using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class DiffSetFacts
    {
        [Fact]
        public void InitializeDiffSetWithChangingIEnumerable()
        {
            var diffSet = new DiffSet<int>(new NonResettingEnumerator().Ints(5));

            Assert.Equal(5, diffSet.GetSnapshot().Count);
            for (int i = 0; i < 5; i++ )
            {
                Assert.True(diffSet.Contains(i));
            }

            var diffPair = diffSet.GetDiff();

            Assert.Equal(5, diffSet.GetSnapshot().Count);
            Assert.Equal(5, diffPair.Added.Count);
            Assert.Equal(0, diffPair.Removed.Count);

            for (int i = 0; i < 5; i++)
            {
                Assert.True(diffSet.Contains(i));
                Assert.True(diffPair.Added.Contains(i));
                Assert.False(diffPair.Removed.Contains(i));
            }
        }

        [Fact]
        public void InitialValueCombineWithChangesInFirstDiff()
        {
            var diffSet = new DiffSet<int>(Enumerable.Range(0, 100));
            Assert.True(diffSet.Add(-1));
            Assert.True(diffSet.Add(100));
            Assert.True(diffSet.Remove(0));
            Assert.True(diffSet.Remove(-1));

            Assert.Equal(100, diffSet.GetSnapshot().Count);

            var diffPair = diffSet.GetDiff();
            Assert.Equal(100, diffPair.Added.Count);
            Assert.Equal(0, diffPair.Removed.Count);

            for (int i = 1; i <= 100; i++)
            {
                Assert.True(diffPair.Added.Contains(i));
                Assert.True(diffSet.Contains(i));
            }
        }

        [Fact]
        public void AddingAndRemovingSameItemMultipleTimesShowsUpOnceInTheDiff()
        {
            var diffSet = new DiffSet<int>(Enumerable.Range(0, 100));

            Assert.False(diffSet.Add(0)); // no-op
            Assert.True(diffSet.Remove(99));
            Assert.False(diffSet.Remove(99)); // no-op

            Assert.Equal(99, diffSet.GetSnapshot().Count);

            var diffPair = diffSet.GetDiff();
            Assert.Equal(99, diffPair.Added.Count);
            Assert.Equal(0, diffPair.Removed.Count);

            for (int i = 0; i < 99; i++)
            {
                Assert.True(diffPair.Added.Contains(i));
                Assert.True(diffSet.Contains(i));
            }

            Assert.False(diffSet.Add(1)); // no-op
            Assert.True(diffSet.Add(99));
            Assert.False(diffSet.Add(99)); // no-op
            Assert.False(diffSet.Remove(101)); // no-op
            Assert.True(diffSet.Remove(0));
            Assert.False(diffSet.Remove(0)); // no-op


            Assert.Equal(99, diffSet.GetSnapshot().Count);

            diffPair = diffSet.GetDiff();
            Assert.Equal(1, diffPair.Added.Count);
            Assert.Equal(1, diffPair.Removed.Count);

            Assert.True(diffPair.Added.Contains(99));
            Assert.True(diffPair.Removed.Contains(0));
            for (int i = 1; i < 100; i++)
            {
                Assert.True(diffSet.Contains(i));
            }
        }

        [Fact]
        public void AddingAndRemovingSameItemDoesNotShowUpInDiff()
        {
            var diffSet = new DiffSet<int>(Enumerable.Range(0, 100));

            Assert.True(diffSet.Add(100));
            Assert.True(diffSet.Remove(98));
            Assert.True(diffSet.Remove(99));
            Assert.True(diffSet.Remove(100));
            Assert.True(diffSet.Add(99));
            Assert.True(diffSet.Add(98));

            Assert.Equal(100, diffSet.GetSnapshot().Count);

            var diffPair = diffSet.GetDiff();
            Assert.Equal(100, diffPair.Added.Count);
            Assert.Equal(0, diffPair.Removed.Count);
            for (int i = 0; i < 100; i++)
            {
                Assert.True(diffPair.Added.Contains(i));
                Assert.True(diffSet.Contains(i));
            }

            Assert.True(diffSet.Add(150));
            Assert.True(diffSet.Add(200));
            Assert.True(diffSet.Remove(50));
            Assert.True(diffSet.Remove(200));
            Assert.True(diffSet.Remove(150));
            Assert.True(diffSet.Add(50));

            Assert.Equal(100, diffSet.GetSnapshot().Count);

            diffPair = diffSet.GetDiff();
            Assert.Equal(0, diffPair.Added.Count);
            Assert.Equal(0, diffPair.Removed.Count);

            for (int i = 0; i < 100; i++)
            {
                Assert.True(diffSet.Contains(i));
            }
        }

        private class NonResettingEnumerator
        {
            private int _start;

            public IEnumerable<int> Ints(int numResults)
            {
                for (int i = _start; i < numResults; i++)
                {
                    yield return i;
                }
                _start += numResults;
            }
        }
    }
}
