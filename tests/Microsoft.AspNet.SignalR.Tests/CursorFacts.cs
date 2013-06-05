using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNet.SignalR.Messaging;
using Xunit;
using Xunit.Extensions;

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

            var serialized = MakeCursor(cursors, prefix: "d-");
            var deserializedCursors = Cursor.GetCursors(serialized, prefix: "d-");

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

            var serialized = MakeCursor(cursors, prefix: "d-");
            var deserializedCursors = Cursor.GetCursors(serialized, prefix: "d-");

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

            var serialized = MakeCursor(manyCursors, prefix: "d-");
            var deserializedCursors = Cursor.GetCursors(serialized, prefix: "d-");

            Assert.Equal(deserializedCursors.Count, 8192);
            for (var i = 0; i < 8192; i++)
            {
                Assert.Equal(manyCursors[i].Id, deserializedCursors[i].Id);
                Assert.Equal(manyCursors[i].Key, deserializedCursors[i].Key);
            }
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

            var serialized = MakeCursor(cursors, prefix: "d-");
            var deserializedCursors = Cursor.GetCursors(serialized, prefix: "d-", keyMaximizer: inverseMap);

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

            var serialized = MakeCursor(cursors, prefix: "d-");
            var deserializedCursors = Cursor.GetCursors(serialized, prefix: "d-", keyMaximizer: sometimesReturnsNull);

            Assert.Null(deserializedCursors);
        }

        [Fact]
        public void GetCursorsAllowsEmptyKey()
        {
            var serializedCursors = @"d-,A";
            var deserializedCursors = Cursor.GetCursors(serializedCursors, prefix: "d-");

            Assert.Equal(1, deserializedCursors.Count);
            Assert.Equal("", deserializedCursors[0].Key);
            Assert.Equal(10UL, deserializedCursors[0].Id);
        }

        [Fact]
        public void CursorWithInvalidSurrogatePair()
        {
            var surrogatePair = "\U0001F4A9";
            var cursorChars = new[] { surrogatePair[0], surrogatePair[1], surrogatePair[0], ',', 'A' };
            var serializedCursor = new StringBuilder("d-").Append(cursorChars).ToString();
            var cursors = Cursor.GetCursors(serializedCursor, "d-");

            Assert.Equal(1, cursors.Count);
            Assert.Equal(3, cursors[0].Key.Length);
            Assert.Equal(10UL, cursors[0].Id);

            cursorChars = new[] { surrogatePair[0], surrogatePair[1], surrogatePair[0], ',', surrogatePair[1], 'A' };
            serializedCursor = new StringBuilder("d-").Append(cursorChars).ToString();

            Assert.Throws<FormatException>(() => Cursor.GetCursors(serializedCursor, prefix: "d-"));
        }

        [Fact]
        public void GetCursorsThrowsGivenDuplicates()
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
            //var serializedCursors = Cursor.MakeCursor(cursors, prefix: "d-");

            var serializedCursors = @"d-\\foo\|1\,4\,\\\|\\\\\\\,,A|\\foo\|1\,4\,\\\|\\\\\\\,,0|\\foo\|1\,4\,\\\|\\\\\\\,,FFFFFFFFFFFFFFFF|,0|,0|"
                                  + @"only non dup,0|,64|ΣιγναλΡ,FFFFFFFFFFFFFFFF|ΣιγναλΡ,FFFFFFFFFFFFFFFF|ΣιγναλΡ,0|\\foo\|1\,4\,\\\|\\\\\\\,,0";

            Assert.Throws<FormatException>(() => Cursor.GetCursors(serializedCursors, prefix: "d-"));
        }

        [Theory]
        [InlineData(@"d-|", "d-")]
        [InlineData(@"d-A|A2|", "d-")]
        [InlineData(@"d-||", "d-")]
        [InlineData(@"d-||,", "d-")]
        [InlineData(@"d-|,|", "d-")]
        [InlineData(@"d-,||", "d-")]
        [InlineData(@"d-,,|", "d-")]
        [InlineData(@"d-,|,", "d-")]
        [InlineData(@"d-,,|", "d-")]
        [InlineData(@"d-A|", "d-")]
        [InlineData(@"d-A||", "d-")]
        [InlineData(@"d-A||,", "d-")]
        [InlineData(@"d-A|,|", "d-")]
        [InlineData(@"d-A,||", "d-")]
        [InlineData(@"d-A,,|", "d-")]
        [InlineData(@"d-A,|,", "d-")]
        [InlineData(@"d-A,,|", "d-")]
        [InlineData(@"d-A|A2", "d-")]
        [InlineData(@"d-A|A", "d-")]
        [InlineData(@"d-A|A2,", "d-")]
        [InlineData(@"d-A|A2|", "d-")]
        [InlineData(@"d-", "d-")]
        [InlineData(@"d-,|,", "d-")]
        [InlineData(@"d-test", "d-")]
        [InlineData(@"d-test,", "d-")]
        [InlineData(@"d-test,A|", "d-")]
        [InlineData(@"d-test,A|,", "d-")]
        [InlineData(@"d-test,,,,,,,,,,,,", "d-")]
        [InlineData(@"d-test,test2,,,,test3,A", "d-")]
        [InlineData(@"d-test,test2,test,A", "d-")]
        [InlineData(@"d-test,A|", "d-")]
        [InlineData(@"d-test,A|random text", "d-")]
        [InlineData(@"d-test,A|test,B", "d-")]
        [InlineData(@"d-,A|,B", "d-")]
        [InlineData(@"d-\test,A", "d-")]
        public void FuzzedCursors(string serializedCursor, string prefix)
        {
            Assert.Throws<FormatException>(() => Cursor.GetCursors(serializedCursor, prefix));
        }

        [Theory]
        [InlineData(@"d-A,|", "d-")]
        [InlineData(@"d-A,|,", "d-")]
        [InlineData(@"d-test,", "d-")]
        [InlineData(@"d-test,A|test2", "d-")]
        [InlineData(@"d-test,A|test2,", "d-")]
        [InlineData(@"d-test,A|test", "d-")]
        [InlineData(@"d-test,A|test,", "d-")]
        [InlineData(@"d-test,A|test,|", "d-")]
        public void CursorsWithoutIds(string serializedCursor, string prefix)
        {
            Assert.Throws<FormatException>(() => Cursor.GetCursors(serializedCursor, prefix));
        }

        [Theory]
        [InlineData(@"d-A,|", "d-")]
        [InlineData(@"d-test,\10", "d-")]
        [InlineData(@"d-test,G", "d-")]
        [InlineData(@"d-test,123ABCQDE", "d-")]
        [InlineData(@"d-test,123ABC\u03A3DE", "d-")]
        [InlineData(@"d-test,123ABC\,DE", "d-")]
        [InlineData(@"d-test,123ABC\\DE", "d-")]
        [InlineData(@"d-test,123ABC\|DE", "d-")]
        [InlineData(@"d-test,123ABC///////|DE", "d-")]
        public void CursorWithInvalidCharactersInIds(string serializedCursor, string prefix)
        {
            Assert.Throws<FormatException>(() => Cursor.GetCursors(serializedCursor, prefix));
        }

        private static unsafe string MakeCursor(IList<Cursor> cursors, string prefix)
        {
            using (var writer = new StringWriter())
            {
                Cursor.WriteCursors(writer, cursors, prefix);
                return writer.ToString();
            }
        }
    }
}
