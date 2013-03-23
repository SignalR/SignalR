using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Messaging;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class ScaleoutStoreFacts
    {
        [Fact]
        public void BinarySearchNoOverwriteSuccess()
        {
            var store = new ScaleoutStore(10);

            for (int i = 0; i < 5; i++)
            {
                store.Add(new ScaleoutMapping((ulong)i, new List<LocalEventKeyInfo>()));
            }

            ArraySegment<ScaleoutMapping> mapping;
            bool result = store.TryBinarySearch(0, out mapping);

            Assert.True(result);
        }

        [Fact]
        public void BinarySearchNoOverwritemBiggerFail()
        {
            var store = new ScaleoutStore(10);

            for (int i = 0; i < 5; i++)
            {
                store.Add(new ScaleoutMapping((ulong)i, new List<LocalEventKeyInfo>()));
            }

            ArraySegment<ScaleoutMapping> mapping;
            bool result = store.TryBinarySearch(20, out mapping);

            Assert.False(result);
        }

        [Fact]
        public void BinarySearchNoOverwritemSmallerFail()
        {
            var store = new ScaleoutStore(10);

            for (int i = 1; i <= 5; i++)
            {
                store.Add(new ScaleoutMapping((ulong)i, new List<LocalEventKeyInfo>()));
            }

            ArraySegment<ScaleoutMapping> mapping;
            bool result = store.TryBinarySearch(0, out mapping);

            Assert.False(result);
        }

        [Fact]
        public void BinarySearchOverwriteSuccess()
        {
            var store = new ScaleoutStore(10);

            int id = 0;
            for (int i = 0; i < store.FragmentSize + 1; i++)
            {
                for (int j = 0; j < store.FragmentCount; j++)
                {
                    store.Add(new ScaleoutMapping((ulong)id, new List<LocalEventKeyInfo>()));
                    id++;
                }
            }

            ArraySegment<ScaleoutMapping> mapping;
            bool result = store.TryBinarySearch(10, out mapping);

            Assert.True(result);
        }

        [Fact]
        public void BinarySearchOverwriteSmallerFail()
        {
            var store = new ScaleoutStore(10);

            int id = 0;
            for (int i = 0; i < store.FragmentSize + 1; i++)
            {
                for (int j = 0; j < store.FragmentCount; j++)
                {
                    store.Add(new ScaleoutMapping((ulong)id, new List<LocalEventKeyInfo>()));
                    id++;
                }
            }

            ArraySegment<ScaleoutMapping> mapping;
            bool result = store.TryBinarySearch(0, out mapping);

            Assert.False(result);
        }

        [Fact]
        public void BinarySearchOverwriteBiggerFail()
        {
            var store = new ScaleoutStore(10);

            int id = 0;
            for (int i = 0; i < store.FragmentSize + 1; i++)
            {
                for (int j = 0; j < store.FragmentCount; j++)
                {
                    store.Add(new ScaleoutMapping((ulong)id, new List<LocalEventKeyInfo>()));
                    id++;
                }
            }

            ArraySegment<ScaleoutMapping> mapping;
            bool result = store.TryBinarySearch(100, out mapping);

            Assert.False(result);
        }
    }
}
