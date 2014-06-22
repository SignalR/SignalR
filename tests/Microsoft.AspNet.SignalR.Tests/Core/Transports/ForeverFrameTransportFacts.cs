using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
using Microsoft.AspNet.SignalR.Transports;
using Moq;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.SignalR.Tests.Core
{
    public class ForeverFrameTransportFacts
    {
        [Theory]
        [InlineData("</sCRiPT>", "\\u003c/sCRiPT\\u003e")]
        [InlineData("</SCRIPT dosomething='false'>", "\\u003c/SCRIPT dosomething='false'\\u003e")]
        [InlineData("<p>ELLO</p>", "\\u003cp\\u003eELLO\\u003c/p\\u003e")]
        public void ForeverFrameTransportEscapesTags(string data, string expected)
        {
            var request = MockRequest();
            var response = new CustomResponse();
            var context = new HostContext(request.Object, response);
            var fft = new ForeverFrameTransport(context, new DefaultDependencyResolver());

            AssertEscaped(fft, response, data, expected);
        }

        [Theory]
        [InlineData("<script type=\"\"></script>", "\\u003cscript type=\"\"\\u003e\\u003c/script\\u003e")]
        [InlineData("<script type=''></script>", "\\u003cscript type=''\\u003e\\u003c/script\\u003e")]
        public void ForeverFrameTransportEscapesTagsWithPersistentResponse(string data, string expected)
        {
            var request = MockRequest();
            var response = new CustomResponse();
            var context = new HostContext(request.Object, response);
            var fft = new ForeverFrameTransport(context, new DefaultDependencyResolver());

            AssertEscaped(fft, response, GetWrappedResponse(data), expected);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("-100")]
        [InlineData("1,000")]
        [InlineData(" ")]
        [InlineData("")]
        [InlineData(null)]
        public void ForeverFrameTransportThrowsOnInvalidFrameId(string frameId)
        {
            var request = new Mock<IRequest>();
            var qs = new NameValueCollection { { "frameId", frameId } };
            request.Setup(r => r.QueryString).Returns(new NameValueCollectionWrapper(qs));
            var response = new CustomResponse();
            var context = new HostContext(request.Object, response);
            var connection = new Mock<ITransportConnection>();
            var fft = new ForeverFrameTransport(context, new DefaultDependencyResolver());

            Assert.Throws(typeof(InvalidOperationException), () => fft.InitializeResponse(connection.Object));
        }

        [Fact]
        public void ForeverFrameTransportSetsCorrectContentType()
        {
            var request = new Mock<IRequest>();
            var qs = new NameValueCollection { { "frameId", "1" } };
            request.Setup(r => r.QueryString).Returns(new NameValueCollectionWrapper(qs));
            var response = new CustomResponse();
            var context = new HostContext(request.Object, response);
            var connection = new Mock<ITransportConnection>();
            var fft = new ForeverFrameTransport(context, new DefaultDependencyResolver());

            fft.InitializeResponse(connection.Object).Wait();

            Assert.Equal("text/html; charset=UTF-8", response.ContentType);
        }

        private static void AssertEscaped(ForeverFrameTransport fft, CustomResponse response, object input, string expectedOutput)
        {
            fft.Send(input).Wait();

            string rawResponse = response.GetData();
            response.Reset();

            // Doing contains due to all the stuff that gets sent through the buffer
            Assert.True(rawResponse.Contains(expectedOutput));
        }

        private static PersistentResponse GetWrappedResponse(string raw)
        {
            var data = Encoding.Default.GetBytes(raw);
            var message = new Message("foo", "key", new ArraySegment<byte>(data));

            var response = new PersistentResponse
            {
                Messages = new List<ArraySegment<Message>> 
                {
                    new ArraySegment<Message>(new Message[] { message })
                }
            };

            return response;
        }

        private static Mock<IRequest> MockRequest()
        {
            var request = new Mock<IRequest>();

            request.SetupGet<INameValueCollection>(r => r.QueryString)
                   .Returns(new NameValueCollectionWrapper());

            return request;
        }

        private class CustomResponse : IResponse
        {
            private MemoryStream _stream;

            public CustomResponse()
            {
                _stream = new MemoryStream();
            }

            public string GetData()
            {
                _stream.Seek(0, SeekOrigin.Begin);
                return new StreamReader(_stream).ReadToEnd();
            }

            public CancellationToken CancellationToken
            {
                get { return CancellationToken.None; }
            }

            public int StatusCode { get; set; }

            public string ContentType { get; set; }

            public void Write(ArraySegment<byte> data)
            {
                _stream.Write(data.Array, data.Offset, data.Count);
            }

            public void Reset()
            {
                _stream.SetLength(0);
            }

            public Task Flush()
            {
                return TaskAsyncHelper.Empty;
            }

            public Task End()
            {
                return TaskAsyncHelper.Empty;
            }
        }
    }
}
