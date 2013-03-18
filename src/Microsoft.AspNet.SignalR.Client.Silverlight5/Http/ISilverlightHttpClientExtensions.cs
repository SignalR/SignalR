using System;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    public static class ISilverlightHttpClientExtensions
    {
        public static void StartAsBrowserStack(this IHttpClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            client.WebRequestFactory = System.Net.Browser.WebRequestCreator.BrowserHttp;
        }
    }
}