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
            Thread.Sleep(TimeSpan.FromSeconds(3));

            requestHandler.Stop();

            // Let all requests finish to see if we get any unintended results
            Thread.Sleep(TimeSpan.FromSeconds(3));
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
            Thread.Sleep(TimeSpan.FromSeconds(3));

            killRequest = true;

            // Let all requests finish to see if we get any unintended results
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }
    }

    public class CustomHttpClient : IHttpClient
    {
        public Task<IResponse> Get(string url, Action<Client.Http.IRequest> prepareRequest)
        {
            throw new NotImplementedException();
        }

        public Task<IResponse> Post(string url, Action<Client.Http.IRequest> prepareRequest, IDictionary<string, string> postData)
        {
            var response = new Mock<IResponse>();

            var mockStream = new MemoryStream();
            var sw = new StreamWriter(mockStream);
            sw.Write("{}");
            sw.Flush();
            mockStream.Position = 0;

            response.Setup(r => r.GetStream()).Returns(mockStream);

            return TaskAsyncHelper.FromResult<IResponse>(response.Object);
        }
    }
}
