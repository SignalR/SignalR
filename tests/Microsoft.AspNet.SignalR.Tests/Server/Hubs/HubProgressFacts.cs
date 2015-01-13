using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests.Server.Hubs
{
    public class HubProgressFacts
    {
        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        [InlineData(typeof(object))]
        [InlineData(typeof(ProgressData))]
        public void HubInvocationCreatesGenericInstanceFromType(Type progressType)
        {
            // Arrange
            var sendProgressFunc = new Func<object, Task>(value => Task.FromResult(true));

            // Act
            var progress = HubInvocationProgress.Create(progressType, sendProgressFunc, traceSource: null);

            // Assert
            var expectedType = typeof(HubInvocationProgress<>).MakeGenericType(progressType);
            Assert.IsType(expectedType, progress);
        }

        [Fact]
        public void HubInvocationProgressSendsProgressValuesViaSendProgressFunc()
        {
            // Arrange
            var result = -1;
            var sendProgressFunc = new Func<object, Task>(value =>
            {
                result = (int)value;
                return Task.FromResult(true);
            });
            var progress = new HubInvocationProgress<int>(sendProgressFunc);

            // Act
            progress.Report(100);

            // Assert
            Assert.Equal(100, result);
        }

        [Fact]
        public void HubInvocationThrowsOnceSetAsComplete()
        {
            // Arrange
            var sendProgressFunc = new Func<object, Task>(value => Task.FromResult(true));
            var progress = new HubInvocationProgress<int>(sendProgressFunc);

            // Act
            progress.SetComplete();

            // Assert
            Assert.Throws<InvalidOperationException>(() => progress.Report(100));
        }

        [Fact]
        public void HubInvocationProgressSendsProgressThenThrowsOnceSetAsComplete()
        {
            // Arrange
            var receivedProgressValue = -1;
            var sendProgressFunc = new Func<object, Task>(value =>
            {
                receivedProgressValue = (int)value;
                return Task.FromResult(true);
            });
            var progress = new HubInvocationProgress<int>(sendProgressFunc);

            // Act
            progress.Report(100);
            progress.SetComplete();

            // Assert
            Assert.Equal(100, receivedProgressValue);
            Assert.Throws<InvalidOperationException>(() => progress.Report(100));
        }

        public class ProgressData
        {
            
        }
    }
}
