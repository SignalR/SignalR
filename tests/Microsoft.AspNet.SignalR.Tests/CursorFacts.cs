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

            var serialized = MakeCursor(cursors);
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

            var serialized = MakeCursor(cursors);
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

            var serialized = MakeCursor(manyCursors);
            var deserializedCursors = Cursor.GetCursors(serialized);

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

            var serialized = MakeCursor(cursors);
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

            var serialized = MakeCursor(cursors);
            var deserializedCursors = Cursor.GetCursors(serialized, sometimesReturnsNull);

            Assert.Null(deserializedCursors);
        }

        [Fact]
        public void GetCursorsAllowsEmptyKey()
        {
            var serializedCursors = @",A";
            var deserializedCursors = Cursor.GetCursors(serializedCursors);

            Assert.Equal(1, deserializedCursors.Count);
            Assert.Equal("", deserializedCursors[0].Key);
            Assert.Equal(10UL, deserializedCursors[0].Id);
        }

        [Fact]
        public void CursorWithInvalidSurrogatePair()
        {
            var surrogatePair = "\U0001F4A9";
            var cursorChars = new[] { surrogatePair[0], surrogatePair[1], surrogatePair[0], ',', 'A' };
            var serializedCursor = new StringBuilder().Append(cursorChars).ToString();
            var cursors = Cursor.GetCursors(serializedCursor);

            Assert.Equal(1, cursors.Count);
            Assert.Equal(3, cursors[0].Key.Length);
            Assert.Equal(10UL, cursors[0].Id);

            cursorChars = new[] { surrogatePair[0], surrogatePair[1], surrogatePair[0], ',', surrogatePair[1], 'A' };
            serializedCursor = new StringBuilder().Append(cursorChars).ToString();

            Assert.Throws<FormatException>(() => Cursor.GetCursors(serializedCursor));
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
            //var serializedCursors = Cursor.MakeCursor(cursors);

            var serializedCursors = @"\\foo\|1\,4\,\\\|\\\\\\\,,A|\\foo\|1\,4\,\\\|\\\\\\\,,0|\\foo\|1\,4\,\\\|\\\\\\\,,FFFFFFFFFFFFFFFF|,0|,0|"
                                  + @"only non dup,0|,64|ΣιγναλΡ,FFFFFFFFFFFFFFFF|ΣιγναλΡ,FFFFFFFFFFFFFFFF|ΣιγναλΡ,0|\\foo\|1\,4\,\\\|\\\\\\\,,0";

            Assert.Throws<FormatException>(() => Cursor.GetCursors(serializedCursors));
        }

        [Theory]
        [InlineData(@"|")]
        [InlineData(@"A|A2|")]
        [InlineData(@"||")]
        [InlineData(@"||,")]
        [InlineData(@"|,|")]
        [InlineData(@",||")]
        [InlineData(@",,|")]
        [InlineData(@",|,")]
        [InlineData(@",,|")]
        [InlineData(@"A|")]
        [InlineData(@"A||")]
        [InlineData(@"A||,")]
        [InlineData(@"A|,|")]
        [InlineData(@"A,||")]
        [InlineData(@"A,,|")]
        [InlineData(@"A,|,")]
        [InlineData(@"A,,|")]
        [InlineData(@"A|A2")]
        [InlineData(@"A|A")]
        [InlineData(@"A|A2,")]
        [InlineData(@"A|A2|")]
        [InlineData(@"")]
        [InlineData(@",|,")]
        [InlineData(@"test")]
        [InlineData(@"test,")]
        [InlineData(@"test,A|")]
        [InlineData(@"test,A|,")]
        [InlineData(@"test,,,,,,,,,,,,")]
        [InlineData(@"test,test2,,,,test3,A")]
        [InlineData(@"test,test2,test,A")]
        [InlineData(@"test,A|")]
        [InlineData(@"test,A|random text")]
        [InlineData(@"test,A|test,B")]
        [InlineData(@",A|,B")]
        [InlineData(@"\test,A")]
        public void FuzzedCursors(string serializedCursor)
        {
            Assert.Throws<FormatException>(() => Cursor.GetCursors(serializedCursor));
        }

        [Theory]
        [InlineData(@"A,|")]
        [InlineData(@"A,|,")]
        [InlineData(@"test,")]
        [InlineData(@"test,A|test2")]
        [InlineData(@"test,A|test2,")]
        [InlineData(@"test,A|test")]
        [InlineData(@"test,A|test,")]
        [InlineData(@"test,A|test,|")]
        public void CursorsWithoutIds(string serializedCursor)
        {
            Assert.Throws<FormatException>(() => Cursor.GetCursors(serializedCursor));
        }

        [Theory]
        [InlineData(@"A,|")]
        [InlineData(@"test,\10")]
        [InlineData(@"test,G")]
        [InlineData(@"test,123ABCQDE")]
        [InlineData(@"test,123ABC\u03A3DE")]
        [InlineData(@"test,123ABC\,DE")]
        [InlineData(@"test,123ABC\\DE")]
        [InlineData(@"test,123ABC\|DE")]
        [InlineData(@"test,123ABC///////|DE")]
        public void CursorWithInvalidCharactersInIds(string serializedCursor)
        {
            Assert.Throws<FormatException>(() => Cursor.GetCursors(serializedCursor));
        }

        private static unsafe string MakeCursor(IList<Cursor> cursors)
        {
            using (var writer = new StringWriter())
            {
                Cursor.WriteCursors(writer, cursors);
                return writer.ToString();
            }
        }
    }
}
