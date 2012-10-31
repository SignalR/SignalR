using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class CursorFacts
    {
        [Fact]
        public void Symmetric()
        {
            var cursors = new[] {
                new Cursor { Id = 10, Key = @"\foo|1,4,\|\\\," },
                new Cursor { Id = 0,  Key = ""},
                new Cursor { Id = 0xffffffffffffffff,  Key = "\u03A3\u03B9\u03B3\u03BD\u03B1\u03BB\u13A1"}
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
        public void SymmetricWithManyCursors()
        {
            var repeatedCursor = new Cursor { Id = 0xffffffffffffffff, Key = Guid.NewGuid().ToString() };
            var manyCursors = Enumerable.Repeat(repeatedCursor, 8192).ToList();

            var serialized = Cursor.MakeCursor(manyCursors);
            var deserializedCursors = Cursor.GetCursors(serialized);

            Assert.Equal(deserializedCursors.Length, 8192);
            for (var i = 0; i < 8192; i++)
            {
                Assert.Equal(repeatedCursor.Id, deserializedCursors[i].Id);
                Assert.Equal(repeatedCursor.Key, deserializedCursors[i].Key);
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

            var cursors = new[] {
                new Cursor { Id = 10, Key = @"\foo|1,4,\|\\\," },
                new Cursor { Id = 0,  Key = ""},
                new Cursor { Id = 0xffffffffffffffff,  Key = "\u03A3\u03B9\u03B3\u03BD\u03B1\u03BB\u13A1"}
            };

            var serialized = Cursor.MakeCursor(cursors, map);
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
            Func<string, string> identity = s => s;
            Func<string, string> sometimesReturnsNull = s => s != "" ? s : null;
            
            var cursors = new[] {
                new Cursor { Id = 10, Key = @"\foo|1,4,\|\\\," },
                new Cursor { Id = 0,  Key = ""},
                new Cursor { Id = 0xffffffffffffffff,  Key = "\u03A3\u03B9\u03B3\u03BD\u03B1\u03BB\u03A1"}
            };

            var serialized = Cursor.MakeCursor(cursors, identity);
            var deserializedCursors = Cursor.GetCursors(serialized, sometimesReturnsNull);

            Assert.Equal(null, deserializedCursors);
        }
    }
}
