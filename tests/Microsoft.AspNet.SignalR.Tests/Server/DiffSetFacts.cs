using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            Assert.True(diffPair.Reset);
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
            diffSet.Add(-1);
            diffSet.Add(100);
            diffSet.Remove(0);
            diffSet.Remove(-1);

            Assert.Equal(100, diffSet.GetSnapshot().Count);

            var diffPair = diffSet.GetDiff();
            Assert.Equal(100, diffPair.Added.Count);
            Assert.Equal(0, diffPair.Removed.Count);
            Assert.True(diffPair.Reset);
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

            diffSet.Add(0); // no-op
            diffSet.Remove(99);
            diffSet.Remove(99); // no-op

            Assert.Equal(99, diffSet.GetSnapshot().Count);

            var diffPair = diffSet.GetDiff();
            Assert.Equal(99, diffPair.Added.Count);
            Assert.Equal(0, diffPair.Removed.Count);
            Assert.True(diffPair.Reset);
            for (int i = 0; i < 99; i++)
            {
                Assert.True(diffPair.Added.Contains(i));
                Assert.True(diffSet.Contains(i));
            }

            diffSet.Add(1); // no-op
            diffSet.Add(99);
            diffSet.Add(99); // no-op
            diffSet.Remove(101); // no-op
            diffSet.Remove(0);
            diffSet.Remove(0); // no-op


            Assert.Equal(99, diffSet.GetSnapshot().Count);

            diffPair = diffSet.GetDiff();
            Assert.Equal(1, diffPair.Added.Count);
            Assert.Equal(1, diffPair.Removed.Count);
            Assert.False(diffPair.Reset);
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

            diffSet.Add(100);
            diffSet.Remove(98);
            diffSet.Remove(99);
            diffSet.Remove(100);
            diffSet.Add(99);
            diffSet.Add(98);

            Assert.Equal(100, diffSet.GetSnapshot().Count);

            var diffPair = diffSet.GetDiff();
            Assert.Equal(100, diffPair.Added.Count);
            Assert.Equal(0, diffPair.Removed.Count);
            Assert.True(diffPair.Reset);
            for (int i = 0; i < 100; i++)
            {
                Assert.True(diffPair.Added.Contains(i));
                Assert.True(diffSet.Contains(i));
            }

            diffSet.Add(150);
            diffSet.Add(200);
            diffSet.Remove(50);
            diffSet.Remove(200);
            diffSet.Remove(150);
            diffSet.Add(50);

            Assert.Equal(100, diffSet.GetSnapshot().Count);

            diffPair = diffSet.GetDiff();
            Assert.Equal(0, diffPair.Added.Count);
            Assert.Equal(0, diffPair.Removed.Count);
            Assert.False(diffPair.Reset);
            for (int i = 0; i < 100; i++)
            {
                Assert.True(diffSet.Contains(i));
            }
        }

        [Fact]
        public void MultipleTasksCanMakeChangesConcurrently()
        {
            var diffSet = new DiffSet<int>(Enumerable.Range(0, 100));

            var diffPair = diffSet.GetDiff();

            Assert.Equal(100, diffPair.Added.Count);
            Assert.Equal(0, diffPair.Removed.Count);
            Assert.True(diffPair.Reset);

            var tasks = new[] {
                Task.Factory.StartNew(() =>
                {
                    for (int i = 0; i < 50; i++)
                    {
                        diffSet.Remove(i);
                    }
                }),
                Task.Factory.StartNew(() =>
                {
                    for (int i = 100; i < 150; i++)
                    {
                        diffSet.Add(i);
                    }
                })
            };

            Task.WaitAll(tasks);

            Assert.Equal(100, diffSet.GetSnapshot().Count);
            for (int i = 50; i < 150; i++)
            {
                Assert.True(diffSet.Contains(i));
            }

            diffPair = diffSet.GetDiff();

            Assert.Equal(50, diffPair.Added.Count);
            Assert.Equal(50, diffPair.Removed.Count);
            Assert.False(diffPair.Reset);
            for (int i = 0; i < 50; i++)
            {
                Assert.False(diffSet.Contains(i));
                Assert.False(diffPair.Added.Contains(i));
                Assert.True(diffPair.Removed.Contains(i));
            }
            for (int i = 50; i < 100; i++)
            {
                Assert.True(diffSet.Contains(i));
                Assert.False(diffPair.Added.Contains(i));
                Assert.False(diffPair.Removed.Contains(i));
            }
            for (int i = 100; i < 50; i++)
            {
                Assert.True(diffSet.Contains(i));
                Assert.True(diffPair.Added.Contains(i));
                Assert.False(diffPair.Removed.Contains(i));
            }
        }

        [Fact]
        public void UsingNullItemsFail()
        {
            Assert.Throws<ArgumentNullException>(() => new DiffSet<string>(new[] {"this", "should", "fail", null}));

            var diffSet = new DiffSet<string>(new[] {"this", "should", "succeed"});

            Assert.Throws<ArgumentNullException>(() => diffSet.Add(null));
            Assert.Throws<ArgumentNullException>(() => diffSet.Remove(null));
            Assert.Throws<ArgumentNullException>(() => diffSet.Contains(null));
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
