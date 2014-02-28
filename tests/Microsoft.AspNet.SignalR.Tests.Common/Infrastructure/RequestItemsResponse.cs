using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Tests.Common.Infrastructure
{
    public class RequestItemsResponse
    {
        public string Method { get; set; }
        public int Count { get; set; }
        public IList<KeyValuePair<string, string>> Headers { get; set; }
        public IList<KeyValuePair<string, string>> Query { get; set; }
        public string[] OwinKeys { get; set; }
        public string XContentTypeOptions { get; set; }

        public override int GetHashCode()
        {
            return Method.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Method.Equals(((RequestItemsResponse)obj).Method);
        }
    }
}
