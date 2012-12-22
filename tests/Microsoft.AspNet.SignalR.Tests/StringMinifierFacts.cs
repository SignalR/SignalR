using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class StringMinifierFacts
    {
        [Fact]
        public void UnminifyReturnsNullIfNotMinifiedValue()
        {
            var stringMinifer = new StringMinifier();
            Assert.Null(stringMinifer.Unminify("invalidValue"));
        }

        [Fact]
        public void Symetric()
        {
            var unminifiedTracker = new Dictionary<string, string>();
            var trickyStrings = new[] { @"\foo|1,4,\|\\\,", "", "\u03A3\u03B9\u03B3\u03BD\u03B1\u03BB\u13A1" };
            var stringMinifer = new StringMinifier();

            foreach (var unminified in trickyStrings)
            {
                unminifiedTracker[stringMinifer.Minify(unminified)] = unminified;
            }

            for (var i = trickyStrings.Length; i < 64; i++)
            {
                var unminified = Guid.NewGuid().ToString();
                unminifiedTracker[stringMinifer.Minify(unminified)] = unminified;
            }

            Assert.Equal(64, unminifiedTracker.Count);
            foreach (var pair in unminifiedTracker)
            {
                Assert.Equal(pair.Value, stringMinifer.Unminify(pair.Key));
            }
            // Ensure unminify can be called multiple times with the same minified value
            foreach (var pair in unminifiedTracker)
            {
                Assert.Equal(pair.Value, stringMinifer.Unminify(pair.Key));
            }
        }

        [Fact]
        public void ReminifiedStringsMinifyToSameResult()
        {
            var unminifiedTracker = new Dictionary<string, string>();
            var trickyStrings = new[] { @"\foo|1,4,\|\\\,", "", "\u03A3\u03B9\u03B3\u03BD\u03B1\u03BB\u13A1" };
            var stringMinifer = new StringMinifier();

            foreach (var unminified in trickyStrings)
            {
                var minified = stringMinifer.Minify(unminified);
                unminifiedTracker[minified] = unminified;
            }

            foreach (var unminified in trickyStrings)
            {
                var minified = stringMinifer.Minify(unminified);
                unminifiedTracker[minified] = unminified;
            }

            Assert.Equal(trickyStrings.Length, unminifiedTracker.Count);
            foreach (var pair in unminifiedTracker)
            {
                Assert.Equal(pair.Value, stringMinifer.Unminify(pair.Key));
            }
        }

        [Fact]
        public void RemovingUnminifiedStringCausesUnminifyToReturnNull()
        {
            var unminifiedTracker = new Dictionary<string, string>();
            var trickyStrings = new[] { @"\foo|1,4,\|\\\,", "", "\u03A3\u03B9\u03B3\u03BD\u03B1\u03BB\u13A1" };
            var stringMinifer = new StringMinifier();

            foreach (var unminified in trickyStrings)
            {
                var minified = stringMinifer.Minify(unminified);
                unminifiedTracker[minified] = unminified;
            }

            var minificationToKeep = stringMinifer.Minify("test");

            foreach (var pair in unminifiedTracker)
            {
                stringMinifer.RemoveUnminified(pair.Value);
            }
            foreach (var pair in unminifiedTracker)
            {
                // There is a memory leak if this does not return null
                Assert.Null(stringMinifer.Unminify(pair.Key));
            }
            Assert.Equal("test", stringMinifer.Unminify(minificationToKeep));
        }

        [Fact]
        public void StringsCanBeReminifiedAfterBeingRemoved()
        {
            var unminifiedTracker1 = new Dictionary<string, string>();
            var unminifiedTracker2 = new Dictionary<string, string>();
            var trickyStrings = new[] { @"\foo|1,4,\|\\\,", "", "\u03A3\u03B9\u03B3\u03BD\u03B1\u03BB\u13A1" };
            var stringMinifer = new StringMinifier();

            foreach (var unminified in trickyStrings)
            {
                var minified = stringMinifer.Minify(unminified);
                unminifiedTracker1[minified] = unminified;
            }

            var minificationToKeep = stringMinifer.Minify("test");

            // Remove trickyStrings before reading them
            foreach (var fullString in trickyStrings)
            {
                stringMinifer.RemoveUnminified(fullString);
            }

            foreach (var unminified in trickyStrings)
            {
                var minified = stringMinifer.Minify(unminified);
                unminifiedTracker2[minified] = unminified;
            }
            foreach (var pair in unminifiedTracker2)
            {
                Assert.Equal(pair.Value, stringMinifer.Unminify(pair.Key));

                // If the first unminifiedTracker contains this key there is likely a memory leak
                Assert.False(unminifiedTracker1.ContainsKey(pair.Key));
            }
            foreach (var pair in unminifiedTracker1)
            {
                // There is a memory leak if this does not return null
                Assert.Null(stringMinifer.Unminify(pair.Key));
            }

            Assert.Equal("test", stringMinifer.Unminify(minificationToKeep));
        }

        [Fact]
        public void UpTo64UniqueStringsMinifyTo1Char()
        {
            var stringMinifer = new StringMinifier();
            var minifiedStrings = new List<string>();

            for (var i = 0; i < 64; i++)
            {
                minifiedStrings.Add(stringMinifer.Minify(Guid.NewGuid().ToString()));
            }

            Assert.Equal(64, minifiedStrings.Count);
            foreach (var minifiedString in minifiedStrings)
            {
                Assert.Equal(1, minifiedString.Length);
            }
        }

        [Fact]
        public void The4097thUniqueStringMinifiesTo3Chars()
        {
            var stringMinifer = new StringMinifier();
            var minifiedStrings = new List<string>();

            for (var i = 0; i < 4096; i++)
            {
                minifiedStrings.Add(stringMinifer.Minify(Guid.NewGuid().ToString()));
            }

            Assert.Equal(4096, minifiedStrings.Count);
            foreach (var minifiedString in minifiedStrings)
            {
                Assert.True(minifiedString.Length <= 2);
            }

            Assert.Equal(3, stringMinifer.Minify(Guid.NewGuid().ToString()).Length);
        }
    }
}
