// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR.Messaging;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Server
{
    public class ScaleoutStoreFacts
    {
        private static readonly ulong MappingIdBase = 100000;

        [Fact]
        public void BinarySearchNoOverwriteSuccess()
        {
            var store = new ScaleoutStore(10);

            for (int i = 0; i < 5; i++)
            {
                store.Add(new ScaleoutMapping(((ulong)i + MappingIdBase), new ScaleoutMessage()));
            }

            ScaleoutStore.Fragment fragment;
            bool result = store.TryGetFragmentFromMappingId(MappingIdBase, out fragment);

            Assert.True(result);
        }

        [Fact]
        public void BinarySearchNoOverwritemBiggerFail()
        {
            var store = new ScaleoutStore(10);

            for (int i = 0; i < 5; i++)
            {
                store.Add(new ScaleoutMapping(((ulong)i + MappingIdBase), new ScaleoutMessage()));
            }

            ScaleoutStore.Fragment fragment;
            bool result = store.TryGetFragmentFromMappingId(MappingIdBase + 20, out fragment);

            Assert.False(result);
        }

        [Fact]
        public void BinarySearchNoOverwritemSmallerFail()
        {
            var store = new ScaleoutStore(10);

            for (int i = 1; i <= 5; i++)
            {
                store.Add(new ScaleoutMapping(((ulong)i + MappingIdBase), new ScaleoutMessage()));
            }

            ScaleoutStore.Fragment fragment;
            bool result = store.TryGetFragmentFromMappingId(MappingIdBase, out fragment);

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
                    store.Add(new ScaleoutMapping(((ulong)id + MappingIdBase), new ScaleoutMessage()));
                    id++;
                }
            }

            ScaleoutStore.Fragment fragment;
            bool result = store.TryGetFragmentFromMappingId(MappingIdBase + 10, out fragment);

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
                    store.Add(new ScaleoutMapping(((ulong)id + MappingIdBase), new ScaleoutMessage()));
                    id++;
                }
            }

            ScaleoutStore.Fragment fragment;
            bool result = store.TryGetFragmentFromMappingId(MappingIdBase, out fragment);

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
                    store.Add(new ScaleoutMapping(((ulong)id + MappingIdBase), new ScaleoutMessage()));
                    id++;
                }
            }

            ScaleoutStore.Fragment fragment;
            bool result = store.TryGetFragmentFromMappingId(MappingIdBase + 100, out fragment);

            Assert.False(result);
        }

        [Fact]
        public void SingleMessageOnlyVerifyIds()
        {
            var store = new ScaleoutStore(10);
            var message = new ScaleoutMessage();
            store.Add(new ScaleoutMapping(10ul, message));

            Assert.Equal(10ul, store.MinMappingId);
            Assert.Equal(10ul, store.MaxMapping.Id);
        }

        [Fact]
        public void AccurateMappingIds()
        {
            var store = new ScaleoutStore(10);
            var message1 = new ScaleoutMessage();
            store.Add(new ScaleoutMapping(10ul, message1));
            var message2 = new ScaleoutMessage();
            store.Add(new ScaleoutMapping(15ul, message2));

            Assert.Equal(10ul, store.MinMappingId);
            Assert.Equal(15ul, store.MaxMapping.Id);
        }

        [Fact]
        public void MinMappingIdMovesWhenOverflow()
        {
            var store = new ScaleoutStore(5);

            int id = 0;
            for (int i = 0; i < store.FragmentSize + 1; i++)
            {
                for (int j = 0; j < store.FragmentCount; j++)
                {
                    var message = new ScaleoutMessage();
                    store.Add(new ScaleoutMapping(((ulong)id + MappingIdBase), message));
                    id++;
                }
            }

            Assert.Equal(((ulong)store.FragmentSize - 1) + MappingIdBase, store.MinMappingId);
        }

        [Fact]
        public void GettingMessagesWithCursorBiggerThanMaxReturnsNothing()
        {
            var store = new ScaleoutStore(10);

            for (int i = 10; i < 15; i++)
            {
                var message = new ScaleoutMessage();
                store.Add(new ScaleoutMapping(((ulong)i + MappingIdBase), message));
            }

            var result = store.GetMessagesByMappingId(MappingIdBase + 16);
            Assert.Equal(0, result.Messages.Count);
        }

        [Fact]
        public void GettingMessagesWithCursorBiggerThanMaxReturnsNothingIfNewer()
        {
            var store = new ScaleoutStore(10);

            for (int i = 0; i < 5; i++)
            {
                var message = new ScaleoutMessage();
                store.Add(new ScaleoutMapping(((ulong)i + MappingIdBase), message));
            }

            var result = store.GetMessagesByMappingId(MappingIdBase + 6);
            Assert.Equal(0, result.Messages.Count);
        }

        [Fact]
        public void GettingMessagesWithCursorLowerThanMinReturnsAll()
        {
            var store = new ScaleoutStore(10);

            for (int i = 5; i < 10; i++)
            {
                var message = new ScaleoutMessage();
                store.Add(new ScaleoutMapping(((ulong)i + MappingIdBase), message));
            }

            var result = store.GetMessagesByMappingId(MappingIdBase + 4);
            Assert.Equal(0ul, result.FirstMessageId);
            Assert.Equal(5ul + MappingIdBase, store.MinMappingId);
            Assert.Equal(5, result.Messages.Count);
        }

        [Fact]
        public void GettingMessagesWithCursorLowerThanMinReturnsAllEvenAfterMultipleOverwrites()
        {
            var store = new ScaleoutStore(10);

            for (int i = 0; i < 100; i++)
            {
                var message = new ScaleoutMessage();
                store.Add(new ScaleoutMapping(((ulong)i + MappingIdBase), message));
            }

            var result = store.GetMessagesByMappingId(MappingIdBase + 62);
            Assert.Equal(64ul, result.FirstMessageId);
            Assert.Equal(63ul + MappingIdBase, store.MinMappingId);
            Assert.Equal(8, result.Messages.Count);
            Assert.True(result.HasMoreData);
        }

        [Fact]
        public void GettingMessagesWithSentinelCursorReturnsEverything()
        {
            var store = new ScaleoutStore(10);

            var message = new ScaleoutMessage();
            store.Add(new ScaleoutMapping((ulong)0, message));

            var result = store.GetMessagesByMappingId(UInt64.MaxValue);
            Assert.Equal(0ul, result.FirstMessageId);
            Assert.Equal(1, result.Messages.Count);
        }

        [Fact]
        public void GettingMessagesWithCursorInbetweenEvenRangeGetsAll()
        {
            AssertMessagesWithCursorForRange(new[] { 1, 4, 6, 10 }, 5, 2ul, 2);
            AssertMessagesWithCursorForRange(new[] { 1, 3, 6, 7, 8, 10 }, 9, 5ul, 1);
        }

        [Fact]
        public void GettingMessagesWithCursorInbetweenOddRangeGetsAll()
        {
            AssertMessagesWithCursorForRange(new[] { 1, 4, 10 }, 2, 1ul, 2);
            AssertMessagesWithCursorForRange(new[] { 1, 3, 6, 8, 10 }, 7, 3ul, 2);
        }

        public void AssertMessagesWithCursorForRange(int[] values, ulong targetId, ulong firstId, int count)
        {
            var store = new ScaleoutStore(10);

            var message = new ScaleoutMessage();
            foreach (var v in values)
            {
                store.Add(new ScaleoutMapping((ulong)v + MappingIdBase, message));
            }

            var result = store.GetMessagesByMappingId(MappingIdBase + targetId);
            Assert.Equal(firstId, result.FirstMessageId);
            Assert.Equal(count, result.Messages.Count);
        }

        [Fact(Skip="Bug #3151")]
        public void GettingMessagesWithCursorInbetweenFragmentsGetsEverythingAfterwards()
        {
            var store = new ScaleoutStore(10, 5);

            var frag1Values = new[] { 1, 2, 3, 4, 5 };
            // Purposely missing '6' between the fragments
            var frag2Values = new[] { 7, 8, 9, 10, 11 };

            foreach (var v in frag1Values.Concat(frag2Values))
            {
                store.Add(new ScaleoutMapping((ulong)v, new ScaleoutMessage()));
            }

            var result = store.GetMessagesByMappingId(6);
            
            Assert.Equal(7ul, result.FirstMessageId);
            Assert.Equal(5, result.Messages.Count);
        }

        [Fact]
        public void GettingMessagesWithCursorInbetweenOnElementRangeGetsAll()
        {
            var store = new ScaleoutStore(10);

            var message = new ScaleoutMessage();
            store.Add(new ScaleoutMapping((ulong)1 + MappingIdBase, message));

            var result = store.GetMessagesByMappingId(2 + MappingIdBase);
            Assert.Equal(0ul, result.FirstMessageId);
            Assert.Equal(0, result.Messages.Count);
        }
    }
}
