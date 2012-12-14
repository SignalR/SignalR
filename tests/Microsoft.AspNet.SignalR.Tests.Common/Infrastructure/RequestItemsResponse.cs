namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure
{
    public class RequestItemsResponse
    {
        public string Method { get; set; }
        public int Count { get; set; }
        public string[] OwinKeys { get; set; }
        public string[] Keys { get; set; }

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
