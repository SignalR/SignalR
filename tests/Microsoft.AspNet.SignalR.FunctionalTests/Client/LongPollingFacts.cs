using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class LongPollingFacts
    {
        [Fact]
        public void PollingRequestHandlerDoesNotPollAfterClose()
        {
            var httpClient = new CustomHttpClient();
            var requestHandler = new PollingRequestHandler(httpClient);
            var active = true;

            Action verifyActive = () =>
            {
                Assert.True(active);
            };

            requestHandler.ResolveUrl = () =>
            {
                Assert.True(active);

                return "";
            };

            requestHandler.PrepareRequest += request =>
            {
                verifyActive();
            };

            requestHandler.OnPolling += verifyActive;

            requestHandler.OnAfterPoll += exception =>
            {
                verifyActive();
                return TaskAsyncHelper.Empty;
            };

            requestHandler.OnError += exception =>
            {
                verifyActive();
            };

            requestHandler.OnMessage += message =>
            {
                verifyActive();
            };

            requestHandler.OnAbort += request =>
            {
                active = false;
            };

            requestHandler.Start();

            // Let the request handler run for three seconds
            Thread.Sleep(TimeSpan.FromSeconds(.1));

            requestHandler.Stop();

            // Let all requests finish to see if we get any unintended results
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PollingRequestHandlerDoesNotPollAfterCloseMidRequest()
        {
            var httpClient = new CustomHttpClient();
            var requestHandler = new PollingRequestHandler(httpClient);
            var active = true;
            var killRequest = false;
            Action verifyActive = () =>
            {
                Assert.True(active);
            };

            requestHandler.ResolveUrl = () =>
            {
                Assert.True(active);

                return "";
            };

            requestHandler.PrepareRequest += request =>
            {
                if (killRequest)
                {
                    // Execute the stop on a different thread so it does not share the lock
                    // This is to simulate a real world situation in which the user requests the connection to stop
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        requestHandler.Stop();
                    });
                }

                verifyActive();
            };

            requestHandler.OnPolling += verifyActive;

            requestHandler.OnMessage += message =>
            {
                verifyActive();
            };

            requestHandler.OnAfterPoll += exception =>
            {
                verifyActive();
                return TaskAsyncHelper.Empty;
            };

            requestHandler.OnError += exception =>
            {
                verifyActive();
            };

            requestHandler.OnAbort += request =>
            {
                active = false;
            };

            requestHandler.Start();

            // Let the request handler run for three seconds
            Thread.Sleep(TimeSpan.FromSeconds(.1));

            killRequest = true;

            // Let all requests finish to see if we get any unintended results
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }
    }

    public class CustomHttpClient : IHttpClient
    {
        public Task<IResponse> Get(string url, Action<Client.Http.IRequest> prepareRequest, bool isLongRunning)
        {
            throw new NotImplementedException();
        }

        public Task<IResponse> Post(string url, Action<Client.Http.IRequest> prepareRequest, IDictionary<string, string> postData, bool isLongRunning)
        {
            var response = new Mock<IResponse>();
            var request = new Mock<Client.Http.IRequest>();
            var mockStream = new MemoryStream();
            var sw = new StreamWriter(mockStream);
            sw.Write("{}");
            sw.Flush();
            mockStream.Position = 0;

            response.Setup(r => r.GetStream()).Returns(mockStream);

            prepareRequest(request.Object);

            return TaskAsyncHelper.FromResult<IResponse>(response.Object);
        }
    }
}
