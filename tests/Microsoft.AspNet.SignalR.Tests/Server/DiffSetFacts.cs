using System.Linq;
using Microsoft.AspNet.SignalR.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class DiffSetFacts
    {
        [Fact]
        public void DetectChangesReturnsFalseIfItemsAlreadyInSet()
        {
            var diffSet = new DiffSet<int>(new[] { 0, 10, 20 });
            diffSet.Add(0);
            diffSet.Add(10);
            diffSet.Add(20);

            var result = diffSet.DetectChanges();

            Assert.False(result);
        }

        [Fact]
        public void DetectChangesReturnsTrueIfItemsNotAlreadyInSet()
        {
            var diffSet = new DiffSet<int>(new[] { 0, 10, 20 });
            diffSet.Add(50);
            diffSet.Add(10);
            diffSet.Add(30);
            diffSet.Remove(10);

            var result = diffSet.DetectChanges();
            var list = diffSet.GetSnapshot().OrderBy(i => i).ToList();

            Assert.True(result);
            Assert.Equal(4, list.Count);
            Assert.Equal(0, list[0]);
            Assert.Equal(20, list[1]);
            Assert.Equal(30, list[2]);
            Assert.Equal(50, list[3]);
        }

        [Fact]
        public void DetectChangesReturnsFalseNotChanged()
        {
            var diffSet = new DiffSet<int>(Enumerable.Empty<int>());
            diffSet.Add(1);
            diffSet.Remove(1);

            var result = diffSet.DetectChanges();

            Assert.False(result);
        }

        [Fact]
        public void DetectChangesReturnsFalseNotChangedAgain()
        {
            var diffSet = new DiffSet<int>(new[] { 1 });
            diffSet.Remove(1);
            diffSet.Add(1);

            var result = diffSet.DetectChanges();
            var items = diffSet.GetSnapshot().ToList();

            Assert.False(result);
            Assert.Equal(1, items[0]);
            Assert.Equal(1, items.Count);
        }

        [Fact]
        public void DetectChangesReturnsTrueIfNoneToSome()
        {
            var diffSet = new DiffSet<int>(Enumerable.Empty<int>());
            diffSet.Add(1);
            diffSet.Remove(1);
            diffSet.Add(5);

            var result = diffSet.DetectChanges();
            var items = diffSet.GetSnapshot().ToList();

            Assert.True(result);
            Assert.Equal(5, items[0]);
        }

        [Fact]
        public void DetectChangesReturnsTrueIfChangedToNothing()
        {
            var diffSet = new DiffSet<int>(new[] { 1 });
            diffSet.Remove(1);

            var result = diffSet.DetectChanges();

            Assert.True(result);
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

            var changes = diffSet.DetectChanges();
            var items = diffSet.GetSnapshot();
            Assert.True(changes);
            Assert.Equal(100, items.Count);
            Assert.False(diffSet.Contains(0));

            for (int i = 1; i <= 100; i++)
            {
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

            var changes = diffSet.DetectChanges();
            Assert.True(changes);

            for (int i = 0; i < 99; i++)
            {
                Assert.True(diffSet.Contains(i));
            }

            Assert.False(diffSet.Add(1)); // no-op
            Assert.True(diffSet.Add(99));
            Assert.False(diffSet.Add(99)); // no-op
            Assert.False(diffSet.Remove(101)); // no-op
            Assert.True(diffSet.Remove(0));
            Assert.False(diffSet.Remove(0)); // no-op


            Assert.Equal(99, diffSet.GetSnapshot().Count);

            changes = diffSet.DetectChanges();
            Assert.True(changes);

            Assert.True(diffSet.Contains(99));
            Assert.False(diffSet.Contains(0));

            for (int i = 1; i < 100; i++)
            {
                Assert.True(diffSet.Contains(i));
            }
        }

        [Fact]
        public void AddingAndRemovingSameItemDoesNotShowUpInDiff()
        {
            // (0-100)
            var diffSet = new DiffSet<int>(Enumerable.Range(0, 100));

            Assert.True(diffSet.Add(100));
            Assert.True(diffSet.Remove(98));
            Assert.True(diffSet.Remove(99));
            Assert.True(diffSet.Remove(100));
            Assert.True(diffSet.Add(99));
            Assert.True(diffSet.Add(98));

            Assert.Equal(100, diffSet.GetSnapshot().Count);

            // (0-100)
            var changes = diffSet.DetectChanges();
            Assert.False(changes);

            for (int i = 0; i < 100; i++)
            {
                Assert.True(diffSet.Contains(i));
            }

            Assert.True(diffSet.Add(150));
            Assert.True(diffSet.Add(200));
            Assert.True(diffSet.Remove(50));
            Assert.True(diffSet.Remove(200));
            Assert.True(diffSet.Remove(150));
            Assert.True(diffSet.Add(50));

            Assert.Equal(100, diffSet.GetSnapshot().Count);

            changes = diffSet.DetectChanges();
            Assert.False(changes);

            for (int i = 0; i < 100; i++)
            {
                Assert.True(diffSet.Contains(i));
            }
        }
    }
}
