﻿using System;
using Microsoft.AspNet.SignalR.Json;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests.Json
{
    public class JsonFacts
    {
        [Fact]
        public void CamelCaseConversPascalCaseToCamelCase()
        {
            // Act
            string name = JsonUtility.CamelCase("SomeMethod");

            // Assert
            Assert.Equal("someMethod", name);
        }

        [Fact]
        public void MimeTypeReturnsJsonMimeType()
        {
            // Act
            string mimeType = JsonUtility.MimeType;

            // Assert
            Assert.Equal("application/json; charset=UTF-8", mimeType);
        }

        [Fact]
        public void JsonPMimeTypeReturnsJsonPMimeType()
        {
            // Act
            string mimeType = JsonUtility.JsonpMimeType;

            // Assert
            Assert.Equal("text/javascript; charset=UTF-8", mimeType);
        }

        [Fact]
        public void CreateJsonPCallbackWrapsContentInMethod()
        {
            // Act
            string callback = JsonUtility.CreateJsonpCallback("foo", "1");

            // Assert
            Assert.Equal("foo(1);", callback);
        }

        [Fact]
        public void CreateJsonPCallbackThrowsWithInvalidIdentifier()
        {
            Assert.Throws(typeof(InvalidOperationException),() => JsonUtility.CreateJsonpCallback("1nogood", "1"));
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("foo.bar")]
        [InlineData("foo.bar.baz")]
        [InlineData("_foo")]
        [InlineData("foo._bar")]
        [InlineData("foo.bar._baz")]
        [InlineData("$foo")]
        [InlineData("foo.$bar")]
        [InlineData("foo.bar.$baz")]
        [InlineData("foo2")]
        [InlineData("foo.bar2")]
        [InlineData("foo.bar.baz2")]
        [InlineData("ۄۺڹ")]
        [InlineData("ۄۺڹ.bar")]
        [InlineData("ۄۺڹ.bar.ۄۺڹ")]
        [InlineData("jQuery18205062005710613621_1359515411213")]
        public void IsValidJavaScriptCallback(string callback)
        {
            // Act
            var isValid = JsonUtility.IsValidJavaScriptCallback(callback);

            // Assert
            Assert.True(isValid);
        }

        [Theory]
        [InlineData("(function evil(x){moreEvil();})")]
        [InlineData("1nogood")]
        [InlineData("<yeahright>")]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("1ۄۺڹ")]
        [InlineData("enum")]
        [InlineData("bar.class.foo")]
        public void InvalidJavaScriptCallback(string callback)
        {
            // Act
            var isValid = JsonUtility.IsValidJavaScriptCallback(callback);

            // Assert
            Assert.False(isValid);
        }
    }
}
