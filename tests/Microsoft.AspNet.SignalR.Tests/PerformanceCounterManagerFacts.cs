using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Tracing;
using Moq;
using Xunit;
using Xunit.Extensions;
using TraceSource = System.Diagnostics.TraceSource;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class PerformanceCounterManagerFacts
    {
        [Theory]
        [InlineData("test#lm/w3svc/421/root", "test-lm-w3svc-421-root")]
        [InlineData(@"test#lm\w3svc\421\root", "test-lm-w3svc-421-root")]
        [InlineData("test(again)", "test[again]")]
        [InlineData("(leading char", "[leading char")]
        [InlineData("trailing char)", "trailing char]")]
        [InlineData("wholly(contained)in-middle", "wholly[contained]in-middle")]
        [InlineData("(leading and trailing)", "[leading and trailing]")]
        [InlineData("longer than 128 chars so it should be truncated0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789",
                    "longer than 128 chars so it should be truncated01234567890123456789012345678901234567890123456789012345678901234567890123456789")]
        [InlineData(@"longer/than\128#chars(so)it should be truncated0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789",
                     "longer-than-128-chars[so]it should be truncated01234567890123456789012345678901234567890123456789012345678901234567890123456789")]
        public void SanitizesInstanceNames(string instanceName, string expectedSanitizedName)
        {
            // Details on how to sanitize instance names are at http://msdn.microsoft.com/en-us/library/vstudio/system.diagnostics.performancecounter.instancename
            
            // Arrange
            var traceManager = new Mock<ITraceManager>();
            traceManager.Setup(tm => tm[It.IsAny<string>()]).Returns<string>(name => new TraceSource(name));

            // Act
            var perfCountersMgr = new PerformanceCounterManager(traceManager.Object);
            perfCountersMgr.Initialize(instanceName, CancellationToken.None);

            // Assert
            Assert.Equal(expectedSanitizedName, perfCountersMgr.InstanceName);
        }
    }
}
