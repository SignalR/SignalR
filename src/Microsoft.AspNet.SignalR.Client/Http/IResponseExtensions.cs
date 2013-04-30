// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Transports;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    public static class IResponseExtensions
    {
        public static Task<string> ReadAsString(this IResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException("response");
            }

            var stream = response.GetStream();
            var reader = new AsyncStreamReader(stream);
            var result = new StringBuilder();
            var resultTcs = new TaskCompletionSource<string>();

            reader.Data = buffer =>
            {
                result.Append(Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count));
            };

            reader.Closed = exception =>
            {
                response.Dispose();
                resultTcs.SetResult(result.ToString());
            };

            reader.Start();

            return resultTcs.Task;
        }
    }
}
