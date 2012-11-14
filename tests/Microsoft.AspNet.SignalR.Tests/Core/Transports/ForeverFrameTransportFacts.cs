using Microsoft.AspNet.SignalR.Transports;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests.Core
{
    public class ForeverFrameTransportFacts
    {
        [Fact]
        public void ForeverFrameTransportEscapesScriptTags()
        {
            var request = new Mock<IRequest>();
            var response = new CustomResponse();
            var context = new HostContext(request.Object, response);

            ForeverFrameTransport fft = new ForeverFrameTransport(context, new DefaultDependencyResolver());

            AssertEscaped(fft, response, "</sCRiPT>", "</\"+\"sCRiPT>");
            AssertEscaped(fft, response, "</SCRIPT dosomething='false'>", "</\"+\"SCRIPT dosomething='false'>");
            AssertEscaped(fft, response, "</scrip>", "</scrip>");
        }

        private static void AssertEscaped(ForeverFrameTransport fft, CustomResponse response, string input, string expectedOutput)
        {
            fft.Send(input).Wait();

            string d = response.GetData();
            response.Reset();

            // Doing contains due to all the crap that gets sent through the buffer
            Assert.True(d.Contains(expectedOutput));
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

            public bool IsClientConnected
            {
                get { return true; }
            }

            public string ContentType { get; set; }

            public void Write(ArraySegment<byte> data)
            {
                foreach (byte b in data.Array)
                {
                    _stream.WriteByte(b);
                }
            }

            public void Reset()
            {
                _stream.Dispose();
                _stream = new MemoryStream();
            }

            public Task FlushAsync()
            {
                return TaskAsyncHelper.Empty;
            }

            public Task EndAsync()
            {
                return TaskAsyncHelper.Empty;
            }
        }
    }
}
