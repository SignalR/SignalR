using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Transports;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Core
{
    public class ForeverFrameTransportFacts
    {
        [Fact]
        public void ForeverFrameTransportEscapesTags()
        {
            var request = new Mock<IRequest>();
            var response = new CustomResponse();
            var context = new HostContext(request.Object, response);
            var fft = new ForeverFrameTransport(context, new DefaultDependencyResolver());

            AssertEscaped(fft, response, "</sCRiPT>", "\\u003c/sCRiPT\\u003e");
            AssertEscaped(fft, response, "</SCRIPT dosomething='false'>", "\\u003c/SCRIPT dosomething='false'\\u003e");
            AssertEscaped(fft, response, "<p>ELLO</p>", "\\u003cp\\u003eELLO\\u003c/p\\u003e");
        }

        private static void AssertEscaped(ForeverFrameTransport fft, CustomResponse response, string input, string expectedOutput)
        {
            fft.Send(input).Wait();

            string rawResponse = response.GetData();
            response.Reset();

            // Doing contains due to all the stuff that gets sent through the buffer
            Assert.True(rawResponse.Contains(expectedOutput));
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
