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

            ScaleoutStore.Fragment fragment;
            bool result = store.TryGetFragmentFromMappingId(0, out fragment);

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

            ScaleoutStore.Fragment fragment;
            bool result = store.TryGetFragmentFromMappingId(20, out fragment);

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

            ScaleoutStore.Fragment fragment;
            bool result = store.TryGetFragmentFromMappingId(0, out fragment);

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

            ScaleoutStore.Fragment fragment;
            bool result = store.TryGetFragmentFromMappingId(10, out fragment);

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

            ScaleoutStore.Fragment fragment;
            bool result = store.TryGetFragmentFromMappingId(0, out fragment);

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

            ScaleoutStore.Fragment fragment;
            bool result = store.TryGetFragmentFromMappingId(100, out fragment);

            Assert.False(result);
        }
    }
}
