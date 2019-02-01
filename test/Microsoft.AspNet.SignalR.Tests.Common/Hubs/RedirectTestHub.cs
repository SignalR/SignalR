namespace Microsoft.AspNet.SignalR.Tests.Common.Hubs
{
    public class RedirectTestHub : Hub
    {
        public string EchoReturn(string message)
        {
            return message;
        }

        public string GetQueryStringValue(string key)
        {
            return Context.QueryString.Get(key);
        }

        public string GetAccessToken()
        {
            var token = Context.Request.QueryString["access_token"];
            if (string.IsNullOrEmpty(token))
            {
                var header = Context.Request.Headers["Authorization"];
                if (!string.IsNullOrEmpty(header) && header.StartsWith("Bearer "))
                {
                    return header.Substring(7); // Strip "Bearer " off the front
                }
            }
            return token;
        }
    }
}
