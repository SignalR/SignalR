﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR.Messaging;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class CursorFacts
    {
        [Fact]
        public void Symmetric()
        {
            var cursors = new[]
            {
                new Cursor(@"\foo|1,4,\|\\\,", 10),
                new Cursor("", 0),
                new Cursor("\u03A3\u03B9\u03B3\u03BD\u03B1\u03BB\u13A1", 0xffffffffffffffff)
            };

            var serialized = Cursor.MakeCursor(cursors);
            var deserializedCursors = Cursor.GetCursors(serialized);

            for (var i = 0; i < cursors.Length; i++)
            {
                Assert.Equal(cursors[i].Id, deserializedCursors[i].Id);
                Assert.Equal(cursors[i].Key, deserializedCursors[i].Key);
            }
        }

        [Fact]
        public void SerializesCorrectlyWithIdEqualTo0()
        {
            var cursors = new[] { new Cursor("", 0) };

            var serialized = Cursor.MakeCursor(cursors);
            var deserializedCursors = Cursor.GetCursors(serialized);

            Assert.Equal(cursors[0].Id, deserializedCursors[0].Id);
        }

        [Fact]
        public void SymmetricWithManyCursors()
        {
            var manyCursors = new List<Cursor>();
            for (var i = 0; i < 8192; i++)
            {
                manyCursors.Add(new Cursor(Guid.NewGuid().ToString(), 0xffffffffffffffff));
            }

            var serialized = Cursor.MakeCursor(manyCursors);
            var deserializedCursors = Cursor.GetCursors(serialized);

            Assert.Equal(deserializedCursors.Length, 8192);
            for (var i = 0; i < 8192; i++)
            {
                Assert.Equal(manyCursors[i].Id, deserializedCursors[i].Id);
                Assert.Equal(manyCursors[i].Key, deserializedCursors[i].Key);
            }
        }

        [Fact]
        public void SymmetricWithNoCursors()
        {
            var manyCursors = new List<Cursor>();

            var serialized = Cursor.MakeCursor(manyCursors);
            var deserializedCursors = Cursor.GetCursors(serialized);

            Assert.Equal(0, deserializedCursors.Length);
        }

        [Fact]
        public void SymmetricWithKeyMap()
        {
            var inverseDict = new Dictionary<string, string>();
            Func<string, string> map = s =>
            {
                var retval = Guid.NewGuid().ToString();
                inverseDict.Add(retval, s);
                return retval;
            };
            Func<string, string> inverseMap = s => inverseDict[s];

            var cursors = new[]
            {
                new Cursor(@"\foo|1,4,\|\\\,", 10, map(@"\foo|1,4,\|\\\,")),
                new Cursor("", 0, map("")),
                new Cursor("\u03A3\u03B9\u03B3\u03BD\u03B1\u03BB\u13A1", 0xffffffffffffffff, map("\u03A3\u03B9\u03B3\u03BD\u03B1\u03BB\u13A1"))
            };

            var serialized = Cursor.MakeCursor(cursors);
            var deserializedCursors = Cursor.GetCursors(serialized, inverseMap);

            for (var i = 0; i < cursors.Length; i++)
            {
                Assert.Equal(cursors[i].Id, deserializedCursors[i].Id);
                Assert.Equal(cursors[i].Key, deserializedCursors[i].Key);
            }
        }

        [Fact]
        public void GetCursorsReturnsNullIfKeyMapReturnsNull()
        {
            Func<string, string> sometimesReturnsNull = s => s != "" ? s : null;

            var cursors = new[]
            {
                new Cursor(@"\foo|1,4,\|\\\,", 10),
                new Cursor("", 0),
                new Cursor("\u03A3\u03B9\u03B3\u03BD\u03B1\u03BB\u03A1", 0xffffffffffffffff)
            };

            var serialized = Cursor.MakeCursor(cursors);
            var deserializedCursors = Cursor.GetCursors(serialized, sometimesReturnsNull);

            Assert.Null(deserializedCursors);
        }

        [Fact]
        public void GetCursorsRemovesDuplicates()
        {
            // The serialized cursors were generated with the following code:
            //var cursors = new[]
            //{
            //    new Cursor(@"\foo|1,4,\|\\\,", 10),
            //    new Cursor(@"\foo|1,4,\|\\\,", 0),
            //    new Cursor(@"\foo|1,4,\|\\\,", 0xffffffffffffffff),
            //    new Cursor("", 0),
            //    new Cursor("", 0),
            //    new Cursor("only non dup", 0),
            //    new Cursor("", 100),
            //    new Cursor("\u03A3\u03B9\u03B3\u03BD\u03B1\u03BB\u03A1", 0xffffffffffffffff),
            //    new Cursor("\u03A3\u03B9\u03B3\u03BD\u03B1\u03BB\u03A1", 0xffffffffffffffff),
            //    new Cursor("\u03A3\u03B9\u03B3\u03BD\u03B1\u03BB\u03A1", 0),
            //    new Cursor(@"\foo|1,4,\|\\\,", 0),
            //};
            //var serializedCursors = Cursor.MakeCursor(cursors);

            var serializedCursors = @"\\foo\|1\,4\,\\\|\\\\\\\,,A|\\foo\|1\,4\,\\\|\\\\\\\,,0|\\foo\|1\,4\,\\\|\\\\\\\,,FFFFFFFFFFFFFFFF|,0|,0|"
                                  + @"only non dup,0|,64|ΣιγναλΡ,FFFFFFFFFFFFFFFF|ΣιγναλΡ,FFFFFFFFFFFFFFFF|ΣιγναλΡ,0|\\foo\|1\,4\,\\\|\\\\\\\,,0";

            var distinctSignals = new[]
            {
                @"\foo|1,4,\|\\\,",
                "",
                "only non dup",
                "\u03A3\u03B9\u03B3\u03BD\u03B1\u03BB\u03A1",
            };
            var deserializedCursors = Cursor.GetCursors(serializedCursors);

            Assert.Equal(distinctSignals.Length, deserializedCursors.Length);
            Assert.True(deserializedCursors.All(c => distinctSignals.Contains(c.Key)));
        }
    }
}
