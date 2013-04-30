using System;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.SqlServer.Tests
{
    public class SqlScaleoutConfigurationFacts
    {
        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(10, true)]
        public void TableCountValidated(int tableCount, bool isValid)
        {
            var config = new SqlScaleoutConfiguration("dummy");

            if (isValid)
            {
                config.TableCount = tableCount;
            }
            else
            {
                Assert.Throws(typeof(ArgumentOutOfRangeException), () => config.TableCount = tableCount);
            }
        }

        [Fact]
        public void ConstructorThrowsForNullConnectionString()
        {
            Assert.Throws(typeof(ArgumentNullException), () => new SqlScaleoutConfiguration(null));
        }

        [Fact]
        public void ConstructorThrowsForEmptyConnectionString()
        {
            Assert.Throws(typeof(ArgumentNullException), () => new SqlScaleoutConfiguration(String.Empty));
        }
    }
}
