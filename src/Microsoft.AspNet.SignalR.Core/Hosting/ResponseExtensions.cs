// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Hosting
{
    /// <summary>
    /// Extension methods for <see cref="IResponse"/>.
    /// </summary>
    public static class ResponseExtensions
    {
        /// <summary>
        /// Closes the connection to a client with optional data.
        /// </summary>
        /// <param name="response">The <see cref="IResponse"/>.</param>
        /// <param name="data">The data to write to the connection.</param>
        /// <returns>A task that represents when the connection is closed.</returns>
        public static Task End(this IResponse response, string data)
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }

            var bytes = Encoding.UTF8.GetBytes(data);
            response.Write(new ArraySegment<byte>(bytes, 0, bytes.Length));
            return response.End();
        }

        /// <summary>
        /// Wraps the underlying <see cref="IResponse"/> object as a stream
        /// </summary>
        /// <param name="response">The <see cref="IResponse"/></param>
        /// <returns>A stream the wraps the response</returns>
        public static Stream AsStream(this IResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }

            return new ResponseStream(response);
        }

        /// <summary>
        /// Stream wrapper around an <see cref="IResponse"/>
        /// </summary>
        private class ResponseStream : Stream
        {
            private readonly IResponse _response;

            public ResponseStream(IResponse response)
            {
                _response = response;
            }

            public override bool CanRead
            {
                get { return false; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return true; }
            }

            public override void Flush()
            {
                
            }

            public override long Length
            {
                get { throw new NotImplementedException(); }
            }

            public override long Position
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _response.Write(new ArraySegment<byte>(buffer, offset, count));
            }
        }
    }
}
