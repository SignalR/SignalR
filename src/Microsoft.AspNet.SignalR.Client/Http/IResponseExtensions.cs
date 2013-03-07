using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    public static class IResponseExtensions
    {
        public static Task<string> ReadAsString(this IResponse response)
        {
            using (Stream stream = response.GetStream())
            {
                var reader = new StreamReader(stream);

                //TODODODODODODO
                return TaskAsyncHelper.FromResult<string>(reader.ReadToEnd());
            }
        }
    }
}
