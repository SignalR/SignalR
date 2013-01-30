using Microsoft.AspNet.SignalR.Owin.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class PrefixMatcherFacts
    {
        [Fact]
        public void ExactMatch()
        {
            // Act
            var result = PrefixMatcher.IsMatch("/echo", "/echo");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void PrefixMatches()
        {
            // Act
            var result = PrefixMatcher.IsMatch("/echo", "/echo/foo");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void InvalidPrefixDoesNotMatch()
        {
            // Act
            var result = PrefixMatcher.IsMatch("/echo", "/echo2/foo");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void LongerPrefixDoesNotMatch()
        {
            // Act
            var result = PrefixMatcher.IsMatch("/echo2/foo/bar", "/echo2/foo");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PrefixWithoutSlashesDoesntMatchIfDifferent()
        {
            // Act
            var result = PrefixMatcher.IsMatch("echo", "echo2");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PrefixWithoutSlashesMatchesIfValidPrefix()
        {
            // Act
            var result = PrefixMatcher.IsMatch("echo", "echo/negotiate");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void EmptyPrefixMatchesEverything()
        {
            // Act
            var result = PrefixMatcher.IsMatch("", "echo/negotiate");

            // Assert
            Assert.True(result);
        }
    }
}
