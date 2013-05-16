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
    }
}
